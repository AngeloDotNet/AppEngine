using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi;

internal class AcceptLanguageHeaderOperationTransformer(IOptions<RequestLocalizationOptions> requestLocalizationOptions) : IOpenApiOperationTransformer
{
    private readonly List<JsonNode>? supportedLanguages = requestLocalizationOptions.Value.SupportedCultures?.Select(c
        => JsonValue.Create(c.Name)).Cast<JsonNode>().ToList();

    private readonly JsonNode defaultLanguage = JsonValue.Create(requestLocalizationOptions.Value.DefaultRequestCulture.Culture.Name)!;

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (supportedLanguages?.Count > 0)
        {
            operation.Parameters ??= [];

            if (!operation.Parameters.Any(p => p.Name == HeaderNames.AcceptLanguage && p.In == ParameterLocation.Header))
            {
                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = HeaderNames.AcceptLanguage,
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema()
                    {
                        Type = JsonSchemaType.String,
                        Enum = supportedLanguages,
                        Default = defaultLanguage
                    }
                });
            }
        }

        return Task.CompletedTask;
    }
}