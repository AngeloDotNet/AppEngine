using AppEngine.Tools.OpenApi.Options;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi.Filters;

internal class OpenApiParametersOperationFilter(OpenApiOperationOptions options) : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (options.Parameters.Count > 0)
        {
            operation.Parameters ??= [];

            foreach (var parameter in options.Parameters
                .Where(parameter => !operation.Parameters.Any(existingParameter
                => existingParameter.Name == parameter.Name && existingParameter.In == parameter.In)))
            {
                operation.Parameters.Add(parameter);
            }
        }

        return Task.CompletedTask;
    }
}