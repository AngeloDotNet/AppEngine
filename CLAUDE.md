# Regole di sviluppo

## Generale
- Definire il plan e salvarlo in un file `.md`
- Il plan va seguito step by step: ogni step richiede un OK esplicito prima di procedere
- **Non eseguire mai commit**
- Unit test e integration test
- Pattern Async/Await, Dependency Injection, Unit of Work
- Aggiungere commenti in inglese ai metodi pubblici, se presenti nel codice in italiano o totalmente assenti, aggiungere i commenti in inglese corrispondenti.
- Per i DTO preferire i `record` alle classi

## Sicurezza
- Considerare sempre la sicurezza: prevenire le vulnerabilità OWASP Top 10 (injection, broken access control, ecc.)
- Proteggere da injection: SQL/NoSQL (query parametrizzate, mai concatenare input), command, LDAP, XSS, header/log injection, path traversal, SSRF, deserializzazione non sicura
- Validare e sanificare ogni input esterno (anche header, query string, route, body)
- Mai esporre dettagli interni (stack trace, messaggi di eccezione) nelle risposte fuori da Development
- Nessun segreto o credenziale nel codice o nei log; confronti di credenziali a tempo costante
- Usare solo API crittografiche moderne e non obsolete
- Tenere i pacchetti NuGet privi di vulnerabilità note (`dotnet list package --vulnerable --include-transitive`)
- Ogni modifica che tocca input, autenticazione o dati va verificata con test di sicurezza (es. input malevoli)

## Qualità del codice
- Seguire i principi SOLID
- Seguire le convenzioni di codifica e gli standard del progetto, inclusi i naming convention, la formattazione, le regole di stile e le best practice del linguaggio utilizzato
- Manutenibile e leggibile
- Nullable reference types abilitati
- Evitare astrazioni non necessarie e overengineering
- Privilegiare la semplicità ogni volta che è possibile
