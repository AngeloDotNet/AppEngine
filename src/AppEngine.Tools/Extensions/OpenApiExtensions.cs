using AppEngine.Tools.OpenApi;
using AppEngine.Tools.OpenApi.Filters;
using AppEngine.Tools.OpenApi.Options;
using AppEngine.Tools.OpenApi.SimpleAuthentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace AppEngine.Tools.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiOperationParameters(this IServiceCollection services, Action<OpenApiOperationOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        var parameters = new OpenApiOperationOptions();
        setupAction.Invoke(parameters);

        services.AddTransient(_ => parameters);

        return services;
    }

    extension(OpenApiOptions options)
    {
        public OpenApiOptions RemoveServerList() => options.AddDocumentTransformer<RemoveServerListDocumentTransformer>();
        public OpenApiOptions AddAcceptLanguageHeader() => options.AddOperationTransformer<AcceptLanguageHeaderOperationTransformer>();
        public OpenApiOptions AddDefaultProblemDetailsResponse() => options.AddOperationTransformer<DefaultResponseOperationTransformer>();
        public OpenApiOptions AddOperationParameters() => options.AddOperationTransformer<OpenApiParametersOperationFilter>();
    }

    //public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName = "Authentication")
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName)
    {
        options.AddSimpleAuthentication(configuration, sectionName, [], []);
    }

    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName,
        IEnumerable<OpenApiSecurityRequirement> addSecurityRequirements, IEnumerable<string> addSecurityDefinitionNames)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        options.AddDocumentTransformer(new AuthenticationDocumentTransformer(configuration, sectionName, addSecurityRequirements, addSecurityDefinitionNames));
        options.AddDocumentTransformer<DefaultResponseDocumentTransformer>();
        options.AddOperationTransformer<AuthenticationOperationTransformer>();
    }
}