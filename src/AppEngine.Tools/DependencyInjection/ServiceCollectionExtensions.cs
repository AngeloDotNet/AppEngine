using System.Text.Json.Serialization;
using AppEngine.Tools.Enums;
using AppEngine.Tools.Extensions;
using AppEngine.Tools.Middleware;
using AppEngine.Tools.Serialization;
using AppEngine.Tools.Settings;
using AppEngine.Tools.Versioning.Transformers;
using AppEngine.Versioning.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace AppEngine.Tools.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures and maps the specified API documentation tool to the application pipeline.
    /// </summary>
    /// <param name="appSettings">Application configuration settings.</param>
    /// <param name="toolDocumentation">The documentation tool to configure.</param>
    /// <param name="swaggerSettings">Configuration settings for Swagger UI.</param>
    /// <param name="scalarSettings">Configuration settings for Scalar.</param>
    /// <param name="app">The web application to configure.</param>
    public static void MapDocumentationTool(AppSettings appSettings, ApiDocumentationTool toolDocumentation, SwaggerSettings swaggerSettings,
        ScalarSettings scalarSettings, WebApplication app)
    {
        if (toolDocumentation is not ApiDocumentationTool.None)
        {
            if (toolDocumentation == ApiDocumentationTool.SwaggerUI)
            {
                if (swaggerSettings.RequireAuthentication)
                {
                    app.UseMiddleware<SwaggerBasicAuthenticationMiddleware>();
                }

                app.MapToolSwaggerUI(appSettings, true);
            }

            if (toolDocumentation == ApiDocumentationTool.Scalar)
            {
                app.MapToolScalar(scalarSettings);
            }
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures HTTP JSON options to ignore null values during serialization, convert enums to strings, and use
        /// UTC date time format.
        /// </summary>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddHttpJsonOptions()
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
            });

            return services;
        }

        /// <summary>
        /// Configures services with a configuration section and returns a bound instance of the configuration object.
        /// </summary>
        /// <typeparam name="T">The type of the configuration object to bind.</typeparam>
        /// <param name="configuration">The configuration to read from.</param>
        /// <param name="sectionName">The name of the configuration section to bind.</param>
        /// <returns>A new instance of <typeparamref name="T"/> with values bound from the configuration section.</returns>
        public T? ConfigureAndGet<T>(IConfiguration configuration, string sectionName) where T : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(sectionName);

            var section = configuration.GetSection(sectionName);
            services.Configure<T>(section);

            var settings = Activator.CreateInstance<T>();
            section.Bind(settings);

            return settings;
        }

        /// <summary>
        /// Adds API versioning and OpenAPI support to the service collection with configurable options.
        /// </summary>
        /// <param name="apiVersions">The array of API version strings to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="apiOptionSettings">The settings that control OpenAPI configuration options.</param>
        /// <param name="apiPolicyOptions">The optional list of API policy settings for version sunset policies.</param>
        /// <returns>The service collection for method chaining.</returns>
        public IServiceCollection AddVersioningApi(string[] apiVersions, IConfiguration configuration, OpenApiOptionSettings apiOptionSettings,
            List<ApiPoliciesSettings> apiPolicyOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(apiVersions);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(apiOptionSettings);

            var apiVersionOptions = new ApiVersioningOptions();
            apiVersionOptions = SetApiVersioningOptions();

            if (apiPolicyOptions != null)
            {
                foreach (var policy in apiPolicyOptions)
                {
                    apiVersionOptions.Policies.Sunset(policy.MajorVersion)
                        .Effective(policy.EffectiveDate)
                        .Link(policy.Link).Title(policy.Title).Type(policy.Type);
                }
            }

            services.AddApiVersioning(options => options = apiVersionOptions)
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                })
                // Rif: https://devblogs.microsoft.com/dotnet/api-versioning-in-dotnet-10-applications/?WT.mc_id=DT-MVP-5005050&utm_content=376554164&utm_medium=social&utm_source=linkedin&hss_channel=lcp-18055275#setting-up-api-versioning-with-openapi-for-minimal-apis
                .AddOpenApi();

            foreach (var description in apiVersions)
            {
                services.AddOpenApi(description, options =>
                {
                    if (apiOptionSettings.RemoveServerList)
                    {
                        // Remove Servers list in OpenAPI.
                        options.RemoveServerList();
                    }

                    if (apiOptionSettings.AddSimpleAuthentication)
                    {
                        // Enable OpenAPI integration for simple authentication.
                        //options.AddSimpleAuthentication(configuration, "JwtSettings");
                        options.AddSimpleAuthentication(configuration, apiOptionSettings.SimpleAuthenticationSectionName);
                    }

                    if (apiOptionSettings.AddAcceptLanguageHeader)
                    {
                        // Add Accept-Language header to all endpoints.
                        options.AddAcceptLanguageHeader();
                    }

                    if (apiOptionSettings.AddDefaultProblemDetailsResponse)
                    {
                        // Add a default (error) response to all endpoints.
                        options.AddDefaultProblemDetailsResponse();
                    }

                    if (apiOptionSettings.AddOperationParameters)
                    {
                        // Enable OpenAPI integration for custom parameters.
                        options.AddOperationParameters();
                    }

                    if (apiOptionSettings.WriteNumberAsString)
                    {
                        // Respect the ignored JsonNumberHandling attribute.
                        options.WriteNumberAsString();
                    }

                    if (apiOptionSettings.DescribeAllParametersInCamelCase)
                    {
                        // Describe all query string parameters in Camel Case.
                        options.DescribeAllParametersInCamelCase();
                    }

                    if (apiOptionSettings.AddTimeExamples)
                    {
                        // Add time examples for TimeSpan and TimeOnly fields.
                        options.AddTimeExamples();
                    }

                    options.AddDocumentTransformer<DocumentInfoDocumentTransformer>();
                    options.AddOperationTransformer<ApiVersionDeprecatedTransformer>();
                });
            }

            return services;
        }

        internal static ApiVersioningOptions SetApiVersioningOptions()
        {
            var options = new ApiVersioningOptions
            {
                ApiVersionReader = new UrlSegmentApiVersionReader(),
                //DefaultApiVersion = new ApiVersion(1),
                //ApiVersionReader = new UrlSegmentApiVersionReader(),
                //ReportApiVersions = true,
                //AssumeDefaultVersionWhenUnspecified = true
            };

            return options;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures Swagger UI with API version endpoints from the application settings.
        /// </summary>
        /// <param name="appSettings">The application settings containing API version information.</param>
        /// <param name="routePrefixSetEmpty">Indicates whether to serve Swagger UI at the application root.</param>
        /// <returns>The configured WebApplication instance.</returns>
        public WebApplication MapToolSwaggerUI(AppSettings appSettings, bool routePrefixSetEmpty = false)
        {
            ArgumentNullException.ThrowIfNull(appSettings);

            var routePrefix = routePrefixSetEmpty ? string.Empty : "swagger";

            app.MapSwaggerUI(routePrefix, options =>
            {
                var descriptions = app.DescribeApiVersions();

                foreach (var description in descriptions)
                {
                    options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", $"{app.Environment.ApplicationName} {description.GroupName}");
                }
            });

            return app;
        }

        /// <summary>
        /// Maps the Scalar API reference UI to the web application with the specified settings.
        /// </summary>
        /// <param name="scalarSettings">The settings to configure the Scalar API reference UI.</param>
        /// <returns>The <see cref="WebApplication"/> instance for method chaining.</returns>
        public WebApplication MapToolScalar(ScalarSettings scalarSettings)
        {
            ArgumentNullException.ThrowIfNull(scalarSettings);

            app.MapScalarApiReference(options =>
            {
                var descriptions = app.DescribeApiVersions();
                var descriptionsCount = descriptions.Count;

                for (var i = 0; i < descriptionsCount; i++)
                {
                    var description = descriptions[i];

                    // In this example, we mark the first API version as the default. Adjust this logic as needed based on your versioning strategy.
                    var isDefault = false;
                    //var isDefault = i == descriptions.Count - 1;

                    if (descriptionsCount > 1)
                    {
                        isDefault = i == 0;
                    }
                    else
                    {
                        isDefault = i == descriptions.Count - 1;
                    }

                    // isDefault is used to mark the default API version in Scalar.
                    // This decides which version is selected by default when users visit the Scalar UI.
                    options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
                }

                options.DarkMode = scalarSettings.DarkMode;
                options.ShowSidebar = scalarSettings.ShowSidebar;
                options.ShowDeveloperTools = scalarSettings.ShowDeveloperToolsVisibility;
                options.Theme = scalarSettings.Theme;
                options.Title = scalarSettings.Title;
                options.WithDefaultHttpClient(scalarSettings.Target, scalarSettings.Client);
            });

            return app;
        }
    }
}