# PLAN – Aggiornamento pacchetti NuGet e miglioramento del codice

> Dopo l'approvazione questo file viene copiato in `E:\Disco_E\Progetti\AppEngine\PLAN.md` (root della soluzione).

## Context
La soluzione (`AppEngine.slnx`, 4 librerie `net10.0`, C# latest, pubblicate come pacchetti `TinyAppEngine*`) ha dipendenze da aggiornare e alcune aree di codice da migliorare (leggibilità, performance, aderenza a `CLAUDE.md`). Non esistono progetti di test e la CI esegue solo `dotnet pack`.

Obiettivo: aggiornare i pacchetti **senza breaking change** per i consumer e migliorare il codice, in step piccoli, ciascuno con OK esplicito.

## Regole di esecuzione
- Un solo step alla volta; dopo ogni step: `dotnet build` + `dotnet test`, riepilogo, **attesa OK esplicito**.
- **Nessun commit** senza OK esplicito. La modifica già presente su `AppEngine.Tools.csproj` non viene toccata/annullata senza averla prima mostrata.
- Commenti XML in inglese sui metodi pubblici; DTO come `record`; async/await, DI, Unit of Work; SOLID; niente overengineering.
- "Nessuna breaking change" = nessuna modifica a firme pubbliche, nomi di tipo/namespace o comportamento osservabile. Ciò che altera il comportamento è nella **Fase 6** ed è opt-in per singolo punto.

## Fase 0 – Baseline e rete di sicurezza
> **Stato: Fase 0 completata. Prossimo passo: Fase 1 (attende OK). Nessun commit effettuato.**
1. `dotnet build` / `dotnet pack` e registrare warning attuali. **[FATTO]** (15 warning registrati: 4 AD0001, 4 CS8601, 5 CS0618, 2 AV0029).
2. Creare i progetti di test (xUnit) in `tests/`: `AppEngine.Tests`, `AppEngine.EFCore.Tests` (EF Core InMemory/SQLite in-memory + `WebApplicationFactory` per gli endpoint CRUD), `AppEngine.Tools.Tests`, `AppEngine.Validations.Tests`. Aggiungerli a `AppEngine.slnx`. **[FATTO]**
3. Scrivere test di caratterizzazione sul comportamento **attuale** delle aree toccate dopo (extension di collection/string/enum/date, `Repository`, `UnitOfWork`, interceptor, `JwtBearerService`, `OperationResult` → HTTP, `ValidatorFilter`, converter JSON). **[FATTO]** (325 test: Core 118, EFCore 73, Tools 119, Validations 15; i test `Current_*` documentano i comportamenti da correggere in Fase 6).
4. Riattivare `dotnet build` e `dotnet test` nei workflow `publish*.yml` (oggi commentati) – solo con OK. **[FATTO]** (build + test attivi nei 4 workflow; il resto del codice commentato è stato lasciato com'è).

## Fase 1 – Aggiornamento pacchetti NuGet (solo patch/minor)
1. `dotnet list package --outdated` (+ `--vulnerable --include-transitive`) e tabella versione corrente → disponibile.
2. Aggiornare solo patch/minor stabili (Microsoft.* 10.0.x, Serilog, FluentValidation 12.x, Asp.Versioning 10.x, Scalar, Swashbuckle, Nerdbank.GitVersioning). Le **major** vengono solo elencate e proposte a parte, mai applicate qui.
3. Pulizia: eliminare il pin duplicato `Nerdbank.GitVersioning` (3.9.50 in `src/Directory.Build.props` vs `Update` 3.10.94 in ogni csproj) tenendo un'unica versione; valutare `Directory.Packages.props` (Central Package Management) per versioni uniche — non cambia il contenuto dei pacchetti pubblicati.
4. Build + test + `dotnet pack`, verificando che le dipendenze nei `.nuspec` generati non alzino i range minimi oltre quanto necessario.

## Fase 2 – Qualità del build (senza impatto runtime)
- `GenerateDocumentationFile=true` + analyzer .NET (`AnalysisLevel=latest-recommended`), warning nullable visibili; `TreatWarningsAsErrors` solo dopo aver azzerato i warning (con OK).
- Rimuovere codice morto/commentato: `AppEngine/DependencyInjection/ServiceCollectionExtensions.cs` (vuoto), righe commentate in `PagedResult.cs`, `OperationResultExtensions.cs`, `ProblemDetailsExtensions.cs`, `ApiPoliciesSettings.cs`, `Tools/DependencyInjection/ServiceCollectionExtensions.cs`, variabili inutilizzate (`routeValueDictionary` in `OperationResultExtensions.cs:29,50`, `inTargetZone` in `DateTimeOffsetExtensions.cs`).
- `sealed` sulle classi non pensate per l'ereditarietà **internal** (ValidatorFilter, DefaultExceptionHandler, transformer interni). Le classi pubbliche restano com'erano (sigillare sarebbe breaking).

## Fase 3 – Scrittura del codice (non-breaking)
- XML doc in inglese su tutti i metodi/tipi pubblici mancanti (EFCore, extension core, `Result`, `JwtBearerService`, settings, converter, `TimeZoneService`); correggere doc errate (`ServiceCollectionExtensions.cs` Tools righe ~94-101 e ~224; tag vuoti in `Validations/.../ServiceCollectionExtensions.cs`).
- Stile: `is null`/`is not null` al posto di `== null` (`UnitOfWork.cs`, `RouteHandlerBuilderExtensions.cs`), rimuovere null-conditional su membri non nullable (`HttpContextEnricher`, `CurrentUserService`, `TimeZoneService`), `HeaderNames.UserAgent`, `const` al posto di `static readonly string`, collection expressions, rimuovere check null ridondanti su primary constructor negli interceptor.
- `ArgumentNullException.ThrowIfNull` nei metodi pubblici di extension privi di guard (`MapDocumentationTool`, ecc.).
- Nullable: sistemare `KeyCloakSettings`, `JwtBearerSettings.SecurityKey`, `BaseEntity.Id` con `required`/inizializzazioni corrette dove non cambia il contratto pubblico; altrimenti annotare e documentare.
- `ConfigureAwait(false)` coerente nelle librerie (come da `copilot.instructions.md`) e propagazione dei `CancellationToken` già disponibili (`AuthenticationOperationTransformer`, `DefaultExceptionHandler`).
- Rimuovere wrapper `async/await` inutili che ritornano un solo `Task` (`Repository.cs`, `UnitOfWork.cs`) → ritorno diretto del `Task`.

## Fase 4 – Performance (non-breaking)
- `JwtBearerService`: cache di `SymmetricSecurityKey`, `TokenValidationParameters` e `JsonWebTokenHandler` (thread-safe) invece di ricrearli a ogni chiamata.
- `OperationResultExtensions`: `OperationResultOptions` di default come istanza statica/cached; mappa status-code in `FrozenDictionary`; evitare `GroupBy/ToDictionary` ridondanti nel percorso errori.
- `ValidatorFilter` (EFCore e Validations): individuare l'indice dell'argomento una volta in fase di costruzione dell'endpoint invece di `FirstOrDefault` + closure a ogni richiesta.
- `Repository.ExistsAsync`: `AnyAsync` invece di `FindAsync` (carica l'entità); `GetPagedAsync` senza comportamento nuovo ma con la query count/dati ordinata documentata.
- `EnumExtensions`: cache di `ResourceManager`/descrizioni (`ConcurrentDictionary`/`FrozenDictionary`), `GetFlags` O(n) con array; `StringExtensions`: `Replace(..., OrdinalIgnoreCase)` e `Contains(..., OrdinalIgnoreCase)` al posto di `Regex`/`IndexOf`, `FirstCharToUpper` con `string.Create`/span e `ToUpperInvariant` (verificare equivalenza con i test della Fase 0).
- `CollectionExtensions`: `IsEmpty`/`GetCount` con `TryGetNonEnumeratedCount`; `Remove` con `RemoveAll` quando `List<T>`; `Array.Empty<T>().AsQueryable()` cached.
- `TimeZoneService`/`ClientTimeProvider`: cache della `TimeZoneInfo` risolta per request.
- `Regex` (se rimasti): `[GeneratedRegex]`.

## Fase 5 – Allineamento a CLAUDE.md senza rompere le API
- DTO nuovi/interni come `record`. Per tipi pubblici esistenti (`PagedResult<T>`, `PaginatedList<T>`, `JwtBearerValidationResult`) **solo proposta** (convertire in record è breaking per chi li istanzia/eredita) → vedi Fase 6.
- Duplicazioni interne: estrarre helper privati nei `DocumentTransformer` (`AddSecurityRequirement` duplicato), unificare le due `StringExtensions` solo se possibile senza cambiare namespace pubblici (altrimenti lasciare e segnalare).
- Aggiornare i README (`README.md` root con struttura `src/` reale, README di progetto) e `.github/instructions` se serve.

## Fase 6 – Candidati con cambio di comportamento (ciascuno richiede OK dedicato, versione minor/major del pacchetto)
Bug/rischi trovati durante l'analisi; correggerli cambia il comportamento osservabile.
I punti **[PRIORITÀ SICUREZZA]** (5, 6) sono da affrontare per primi; i test di caratterizzazione della Fase 0 includono casi di input malevoli per questi componenti.
1. `Tools/DependencyInjection/ServiceCollectionExtensions.cs:123` – `AddApiVersioning(options => options = apiVersionOptions)` scarta la configurazione (reader URL segment, policy). La correzione cambia le route/versioning dei consumer.
2. `DateTimeOffsetExtensions.ToTimeOnly` ignora il parametro `zone`.
3. `Repository` salva a ogni scrittura, vanificando `IUnitOfWork`: spostare il salvataggio sulla UoW è breaking; alternativa non-breaking: documentare/aggiungere overload.
4. Interceptor Audit/SoftDelete solo `SavingChangesAsync`: aggiungere `SavingChanges` sync (materializzare `Entries.ToList()` prima di cambiare lo stato).
5. **[PRIORITÀ SICUREZZA]** `SwaggerBasicAuthenticationMiddleware`: base64 malformato → 401 (oggi 500), confronto credenziali a tempo costante.
6. **[PRIORITÀ SICUREZZA]** `DefaultExceptionHandler`/`ProblemDetailsExtensions`: non esporre tipo e messaggio dell'eccezione fuori da Development; status 500 esplicito.
7. `GetPagedAsync` senza `OrderBy` (pagine non deterministiche); `GenericCrudEndpoints GET /` senza paginazione.
8. `CollectionExtensions.DistinctBy/Chunk/ToListAsync` che oscurano/collidono con la BCL/EF Core.
9. `AddPersistenceUoW` registra `IUnitOfWork` a ogni chiamata; `AddPersistence` duplicato.
10. `TimeExampleSchemaTransformer` con `DateTime.Now` (output OpenAPI non deterministico); introdurre `TimeProvider` negli audit.
11. Tipi paginazione duplicati (`PagedResult` vs `PaginatedList`), `Result` con `operator true/false`.
12. Pacchetti **major** emersi dalla Fase 1.
13. Warning di baseline `CS0618` (API OpenAPI obsolete; cambiano l'output del documento OpenAPI): `OpenApiSchema.Example` → `Examples` in `WriteNumberAsStringSchemaTransformer.cs:51` e `TimeExampleSchemaTransformer.cs:15`; `OpenApiParameter.AllowEmptyValue` in `CamelCaseQueryParametersOperationTransformer.cs:31`.
15. **[PRIORITÀ SICUREZZA]** `Result.Fail(reason, Exception)` / `ErrorMessage` / `ErrorDetail` (`OperationResults/Result.cs`, `Result{OfT}.cs`) copiano messaggio e inner message dell'eccezione in title/detail della risposta HTTP (`OperationResultExtensions.Problem`), in ogni ambiente. Emerso scrivendo i test di Fase 0 (`Current_Result_Fail_WithException_ExposesExceptionMessageToClient`).
16. Codice irraggiungibile in `OperationResultExtensions.Problem`: `Type ??= "https://httpstatuses.io/{status}"` non viene mai usato perché `TypedResults.Problem` valorizza già `Type` con l'URL RFC; da decidere se rimuovere il fallback (non cambia l'output) o forzare il valore voluto (lo cambia).
14. Warning di baseline `AV0029`: `AddApiVersioning().AddOpenApi()` in `Tools/DependencyInjection/ServiceCollectionExtensions.cs:134` (da valutare insieme al punto 1, stessa area).
17. Interceptor dipendenti dall'ordine di registrazione: se `AuditSaveChangesInterceptor` gira prima di `SoftDeleteInterceptor`, la cancellazione logica non valorizza `UpdatedAt/UpdatedBy`. In più la libreria non registra alcun query filter per `ISoftDelete`, quindi le righe cancellate restano visibili (da documentare o aggiungere helper opt-in). Fissato dai test `Current_SoftDelete_*` in `AppEngine.EFCore.Tests`.
18. Emersi dai test del Core (`AppEngine.Tests`, test `Current_*`):
    - `StringExtensions.ReplaceIgnoreCase` passa la sostituzione a `Regex.Replace`, quindi `$&`, `$1` ecc. non sono letterali. **Attenzione in Fase 4**: sostituirlo con `string.Replace(..., OrdinalIgnoreCase)` cambia questo comportamento; farlo solo con OK.
    - `StringExtensions.FirstCharToUpper` usa `ToUpper()` sensibile alla cultura (in turco `i` → `İ`); usare `ToUpperInvariant` cambia l'output.
    - `EnumExtensions.GetDescriptions` lancia `InvalidCastException` per enum non `int` e `ArgumentException` se due membri hanno lo stesso valore.
    - `HttpContextEnricher` scrive lo `User-Agent` senza sanificare CR/LF (rischio log forging sui sink testuali). **[PRIORITÀ SICUREZZA]**

## Verifica end-to-end
- `dotnet restore && dotnet build -c Release` senza nuovi warning; `dotnet test` verde ad ogni step.
- `dotnet pack -c Release` e ispezione dei `.nupkg` (dipendenze/versioni, README, doc XML presente).
- Confronto API pubblica prima/dopo (es. `Microsoft.CodeAnalysis.PublicApiAnalyzers` o `dotnet-apicompat`) per provare l'assenza di breaking change nelle Fasi 1-5.
- Smoke test con `WebApplicationFactory`: endpoint CRUD, risposta errori/ProblemDetails, OpenAPI document (snapshot), JWT create/validate/refresh.
- `dotnet list package --outdated --vulnerable --include-transitive` finale.

## Fase 7 – Aggiornamento documentazione README (ultimo punto del piano)
Da eseguire alla fine, quando API e comportamento sono definitivi, così la documentazione riflette lo stato finale. Modello di riferimento: <https://github.com/marcominerva/TinyHelpers> (root `README.md` e `src/TinyHelpers.*/README.md`).

**Struttura da seguire** (stile pratico e conciso, frasi brevi, tabelle per l'API, snippet C# brevi):
1. Titolo + riga di badge shields.io (lint, CodeQL se presente, NuGet version, NuGet downloads, licenza).
2. Introduzione breve (cosa fa il pacchetto, aree funzionali).
3. **Compatibility**: elenco dei framework supportati (`net10.0`) con note sulle API che richiedono versioni specifiche.
4. **Installation**: `dotnet add package <PackageId>` + alternativa Visual Studio / NuGet Package Manager (e nota sulla sorgente Baget se ancora valida).
5. **Contents**: indice con link alle sezioni.
6. Sezioni per area funzionale: una riga di scopo, tabella (metodo | cosa fa | quando usarlo), snippet con i `using` necessari.
7. **Quick examples**: pattern d'uso più comuni in forma breve.
8. **Contribute**: invito a contribuire/segnalare issue, con avviso sul branch di destinazione delle PR (verificare qual è il branch corretto del repo, oggi `main`).

**File da produrre/aggiornare**
- `README.md` (root): panoramica della soluzione, compatibilità, installazione, elenco dei pacchetti con link ai rispettivi README ("Other package documentation"), contribute. Correggere l'albero di cartelle obsoleto (`TinyAppEngine/…`) con la struttura reale `src/` + `tests/`.
- `src/AppEngine/README.md` – **da creare** (oggi il core pacchettizza il README di root): estensioni collection/string/enum/date/guid/comparable, routing (`MapEndpoints`, helper `RouteHandlerBuilder`), logging enricher Serilog. Con OK, il csproj passerà a pacchettizzare il proprio README.
- `src/AppEngine.EFCore/README.md` – aggiornare: entità base, repository, Unit of Work, interceptor audit/soft delete, endpoint CRUD generici, `ApplyMigrations`, registrazione DI.
- `src/AppEngine.Tools/README.md` – aggiornare: documentazione API (Swagger UI / Scalar), versioning, OpenAPI transformer, exception handler/ProblemDetails, OperationResult, autenticazione JWT/KeyCloak, localizzazione, time zone, converter JSON.
- `src/AppEngine.Validations/README.md` – aggiornare: `WithValidation`, `AddValidatorsFromAssembly`, `ConfigureValidation`/`ValidationOptions`.
- Eventuale correzione dei README in base alle modifiche comportamentali approvate in Fase 6 (es. gestione errori, sicurezza Swagger).

**Regole**
- Contenuti in inglese (coerenti con i commenti XML e con i README esistenti dei pacchetti); gli esempi devono compilare: ogni snippet viene verificato (build di un progetto sample temporaneo o test) prima della consegna.
- Nessun segreto o credenziale reale negli esempi (usare placeholder); evidenziare le raccomandazioni di sicurezza (es. non esporre Swagger senza autenticazione).
- Link relativi interni verificati, badge puntati ai workflow/pacchetti reali.
- Verifica: `dotnet pack` e controllo che ogni `.nupkg` contenga il README del proprio progetto.

## Ordine proposto
Fase 0 → 1 → 2 → 3 → 4 → 5 → (Fase 6 punto per punto, su scelta) → **Fase 7 (README, ultimo punto)**. Stop e attesa OK dopo ogni fase/step.
