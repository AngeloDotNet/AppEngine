using AppEngine.EFCore.Endpoints.Validations;
using AppEngine.EFCore.Repositories.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AppEngine.EFCore.Endpoints;

/// <summary>
/// Provides helper methods for mapping conventional CRUD endpoints for an entity type.
/// </summary>
public static class GenericCrudEndpoints
{
    /// <summary>
    /// Maps a standard set of CRUD endpoints for the specified entity and DTO types.
    /// </summary>
    /// <typeparam name="TEntity">The entity type handled by the endpoints.</typeparam>
    /// <typeparam name="TKey">The entity identifier type.</typeparam>
    /// <typeparam name="TReadDto">The DTO type returned by read operations.</typeparam>
    /// <typeparam name="TCreateDto">The DTO type accepted by create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type accepted by update operations.</typeparam>
    /// <param name="app">The endpoint route builder used to register the routes.</param>
    /// <param name="routePrefix">The route prefix for the generated endpoint group.</param>
    /// <param name="tags">The tag assigned to the endpoint group for API metadata.</param>
    /// <param name="validationGroupName">The base name used to generate endpoint names.</param>
    /// <param name="idSelector">A function that selects the entity identifier.</param>
    /// <param name="toReadDto">A function that converts an entity to its read DTO representation.</param>
    /// <param name="toEntityFromCreate">A function that converts a create DTO into a new entity instance.</param>
    /// <param name="applyUpdate">A function that applies an update DTO to an existing entity instance.</param>
    /// <returns>The created <see cref="RouteGroupBuilder"/> containing the mapped CRUD endpoints.</returns>
    /// <remarks>
    /// The generated endpoints include:
    /// <list type="bullet">
    /// <item><description><c>GET /</c> to retrieve all entities.</description></item>
    /// <item><description><c>GET /{id}</c> to retrieve a single entity by identifier.</description></item>
    /// <item><description><c>POST /</c> to create a new entity.</description></item>
    /// <item><description><c>PUT /{id}</c> to update an existing entity.</description></item>
    /// <item><description><c>DELETE /{id}</c> to delete an existing entity.</description></item>
    /// </list>
    /// </remarks>
    public static RouteGroupBuilder MapCrudEndpoints<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>(this IEndpointRouteBuilder app, string routePrefix,
        string tags, string validationGroupName, Func<TEntity, TKey> idSelector, Func<TEntity, TReadDto> toReadDto, Func<TCreateDto, TEntity> toEntityFromCreate,
        Action<TEntity, TUpdateDto> applyUpdate) where TEntity : class where TKey : notnull where TCreateDto : class where TUpdateDto : class
    {
        var entityName = typeof(TEntity).Name;
        var group = app.MapGroup(routePrefix).WithTags(tags);

        // Get all
        group.MapGet("/", async (IRepository<TEntity, TKey> repo, CancellationToken ct) =>
        {
            var items = await repo.GetAllAsync(ct);
            return Results.Ok(items.Select(toReadDto).ToList());
        })
        .WithName($"{validationGroupName}_GetAll")
        .WithDescription($"Gets all {entityName} entities.")
        .WithSummary($"Gets all {entityName} entities.");

        // Get by id
        group.MapGet("/{id}", async (TKey id, IRepository<TEntity, TKey> repo, CancellationToken ct) =>
        {
            var item = await repo.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(toReadDto(item));
        })
        .WithName($"{validationGroupName}_GetById")
        .WithDescription($"Gets a {entityName} entity by its ID.")
        .WithSummary($"Gets a {entityName} entity by its ID.");

        // Create
        group.MapPost("/", async ([FromBody] TCreateDto dto, IRepository<TEntity, TKey> repo, CancellationToken ct) =>
        {
            var entity = toEntityFromCreate(dto);
            await repo.AddAsync(entity, ct);

            return Results.Created($"{routePrefix}/{idSelector(entity)}", toReadDto(entity));
        })
        .AddEndpointFilter<ValidatorFilter<TCreateDto>>().ProducesValidationProblem()
        .WithName($"{validationGroupName}_Create")
        .WithDescription($"Creates a new {entityName} entity.")
        .WithSummary($"Creates a new {entityName} entity.");

        // Update
        group.MapPut("/{id}", async (TKey id, [FromBody] TUpdateDto dto, IRepository<TEntity, TKey> repo, CancellationToken ct) =>
        {
            var entity = await repo.GetByIdAsync(id, ct);

            if (entity is null)
            {
                return Results.NotFound();
            }

            applyUpdate(entity, dto);
            await repo.UpdateAsync(entity, ct);

            return Results.NoContent();
        })
        .AddEndpointFilter<ValidatorFilter<TUpdateDto>>().ProducesValidationProblem()
        .WithName($"{validationGroupName}_Update")
        .WithDescription($"Updates an existing {entityName} entity.")
        .WithSummary($"Updates an existing {entityName} entity.");

        // Delete
        group.MapDelete("/{id}", async (TKey id, IRepository<TEntity, TKey> repo, CancellationToken ct) =>
        {
            var entity = await repo.GetByIdAsync(id, ct);

            if (entity is null)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(entity, ct);
            return Results.NoContent();
        })
        .WithName($"{validationGroupName}_Delete")
        .WithDescription($"Deletes an existing {entityName} entity.")
        .WithSummary($"Deletes an existing {entityName} entity.");

        return group;
    }
}