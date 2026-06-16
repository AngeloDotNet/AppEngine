using AppEngine.Tools.OpenApi;
using AppEngine.Tools.OpenApi.Filters;
using AppEngine.Tools.OpenApi.Options;
using AppEngine.Tools.OpenApi.SimpleAuthentication;
using AppEngine.Tools.Settings;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace AppEngine.Tools.Extensions;

public static class OpenApiExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenApiOperationParameters(Action<OpenApiOperationOptions> setupAction)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(setupAction);

            var parameters = new OpenApiOperationOptions();
            setupAction.Invoke(parameters);

            services.AddTransient(_ => parameters);

            return services;
        }
    }

    extension(OpenApiOptions options)
    {
        public OpenApiOptions RemoveServerList() => options.AddDocumentTransformer<RemoveServerListDocumentTransformer>();
        public OpenApiOptions AddAcceptLanguageHeader() => options.AddOperationTransformer<AcceptLanguageHeaderOperationTransformer>();
        public OpenApiOptions AddDefaultProblemDetailsResponse() => options.AddOperationTransformer<DefaultResponseOperationTransformer>();
        public OpenApiOptions AddOperationParameters() => options.AddOperationTransformer<OpenApiParametersOperationFilter>();
        public OpenApiOptions WriteNumberAsString() => options.AddSchemaTransformer<WriteNumberAsStringSchemaTransformer>();
        public OpenApiOptions DescribeAllParametersInCamelCase() => options.AddOperationTransformer<CamelCaseQueryParametersOperationTransformer>();
        public OpenApiOptions AddTimeExamples() => options.AddSchemaTransformer<TimeExampleSchemaTransformer>();

        public void AddSimpleAuthentication(IConfiguration configuration, string sectionName)
        {
            options.AddSimpleAuthentication(configuration, sectionName, [], []);
        }

        public void AddSimpleAuthentication(IConfiguration configuration, string sectionName,
            IEnumerable<OpenApiSecurityRequirement> addSecurityRequirements, IEnumerable<string> addSecurityDefinitionNames)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

            options.AddDocumentTransformer(new AuthenticationDocumentTransformer(configuration, sectionName, addSecurityRequirements, addSecurityDefinitionNames));
            options.AddDocumentTransformer<DefaultResponseDocumentTransformer>();
            options.AddOperationTransformer<AuthenticationOperationTransformer>();
        }

        public void AddKeyCloakAuthentication(KeyCloakSettings settings)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(settings);

            options.AddKeyCloakAuthentication(settings.Name, new()
            {
                AuthorizationUrl = new(settings.AuthorizationUrl),
                TokenUrl = new(settings.TokenUrl),
                Scopes = settings.Scopes
            });
        }

        internal void AddKeyCloakAuthentication(string name, OpenApiOAuthFlow authFlow)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(authFlow);

            options.AddDocumentTransformer(new KeyCloakAuthenticationDocumentTransformer(name, authFlow));
        }
    }
}