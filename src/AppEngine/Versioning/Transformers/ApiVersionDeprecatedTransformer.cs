using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Versioning.Transformers;

public class ApiVersionDeprecatedTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Deprecated |= context.Description.IsDeprecated;
        return Task.CompletedTask;
    }
}