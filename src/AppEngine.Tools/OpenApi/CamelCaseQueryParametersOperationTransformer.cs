using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi;

public class CamelCaseQueryParametersOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Ensures that query parameters are camel-cased in the OpenAPI document.
        if (operation.Parameters is not null)
        {
            for (var i = 0; i < operation.Parameters.Count; i++)
            {
                var parameter = operation.Parameters[i];

                if (parameter.In == ParameterLocation.Query)
                {
                    var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);

                    if (parameter.Name != camelCaseName)
                    {
                        operation.Parameters[i] = new OpenApiParameter
                        {
                            Name = camelCaseName,
                            In = parameter.In,
                            Description = parameter.Description,
                            Required = parameter.Required,
                            Deprecated = parameter.Deprecated,
                            AllowEmptyValue = parameter.AllowEmptyValue,
                            Style = parameter.Style,
                            Explode = parameter.Explode,
                            AllowReserved = parameter.AllowReserved,
                            Schema = parameter.Schema,
                            Example = parameter.Example,
                            Examples = parameter.Examples,
                            Content = parameter.Content,
                            Extensions = parameter.Extensions
                        };
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}