using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi.SimpleAuthentication;

internal class DefaultResponseDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Generate schema for error responses.
        var problemDetailsSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
        document.AddComponent(nameof(ProblemDetails), problemDetailsSchema);
    }
}