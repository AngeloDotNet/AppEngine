using AppEngine.Tools.OpenApi.Helpers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi.SimpleAuthentication;

internal class OAuth2AuthenticationDocumentTransformer(string name, OpenApiOAuthFlow authFlow) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add(name, new OpenApiSecurityScheme()
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new()
            {
                AuthorizationCode = authFlow
            }
        });

        AddSecurityRequirement(document, name);

        return Task.CompletedTask;
    }

    internal static void AddSecurityRequirement(OpenApiDocument document, string name)
        => AddSecurityRequirement(document, OpenApiHelpers.CreateSecurityRequirement(name, document));

    private static void AddSecurityRequirement(OpenApiDocument document, OpenApiSecurityRequirement requirement)
    {
        document.Security ??= [];
        document.Security.Add(requirement);
    }
}