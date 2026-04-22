using AppEngine.Tools.OpenApi.Helpers;
using AppEngine.Tools.SimpleAuthentication.JwtBearer.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace AppEngine.Tools.OpenApi.SimpleAuthentication;

internal class AuthenticationDocumentTransformer(IConfiguration configuration, string sectionName, IEnumerable<OpenApiSecurityRequirement> additionalSecurityRequirements, IEnumerable<string> additionalSecurityDefinitionNames) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(document, configuration.GetSection($"{sectionName}:JwtBearer"));

        if (additionalSecurityRequirements.Any())
        {
            // Adds all the other security requirements that have been specified.
            foreach (var securityRequirement in additionalSecurityRequirements)
            {
                AddSecurityRequirement(document, securityRequirement);
            }
        }

        if (additionalSecurityDefinitionNames.Any())
        {
            // Adds all the other security definitions that have been specified.
            foreach (var definitionName in additionalSecurityDefinitionNames)
            {
                AddSecurityRequirement(document, definitionName);
            }
        }

        return Task.CompletedTask;

        static void CheckAddJwtBearer(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityScheme(document, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token", "JWT");
            AddSecurityRequirement(document, settings.SchemeName);
        }
    }

    private static void AddSecurityScheme(OpenApiDocument document, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description, string? bearerFormat = null)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add(name, new OpenApiSecurityScheme()
        {
            In = location,
            Name = parameterName,
            Description = description,
            Type = securitySchemeType,
            Scheme = scheme,
            BearerFormat = bearerFormat
        });
    }

    internal static void AddSecurityRequirement(OpenApiDocument document, string name)
        => AddSecurityRequirement(document, OpenApiHelpers.CreateSecurityRequirement(name, document));

    private static void AddSecurityRequirement(OpenApiDocument document, OpenApiSecurityRequirement requirement)
    {
        document.Security ??= [];
        document.Security.Add(requirement);
    }
}