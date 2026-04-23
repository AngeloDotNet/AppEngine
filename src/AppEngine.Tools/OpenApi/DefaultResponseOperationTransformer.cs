using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi;

public class DefaultResponseOperationTransformer : IOpenApiOperationTransformer
{
    public string DefaultResponseCode { get; set; } = "default";

    public string DefaultDescription { get; set; } = "Error";

    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Ensure ProblemDetails schema is generated.
        var problemDetailsSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
        context.Document?.AddComponent(nameof(ProblemDetails), problemDetailsSchema);

        // Reference the schema in responses.
        operation.Responses ??= [];
        operation.Responses.TryAdd(DefaultResponseCode, new OpenApiResponse
        {
            Description = DefaultDescription,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypeNames.Application.ProblemJson] = new()
                {
                    Schema = new OpenApiSchemaReference(nameof(ProblemDetails), context.Document)
                }
            }
        });
    }
}