using System.Text.Json.Serialization;
using AppEngine.Tools.Extensions;
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
                        .Link(policy.Link)
                        .Title(policy.Title).Type(policy.Type);
                }
            }

            //services.AddApiVersioning()
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
                    // Remove Servers list in OpenAPI.
                    //options.RemoveServerList();

                    if (apiOptionSettings.RemoveServerList)
                    {
                        // Remove Servers list in OpenAPI.
                        options.RemoveServerList();
                    }

                    // Enable OpenAPI integration for simple authentication.
                    //options.AddSimpleAuthentication(builder.Configuration, "JwtSettings");

                    if (apiOptionSettings.AddSimpleAuthentication)
                    {
                        // Enable OpenAPI integration for simple authentication.
                        //options.AddSimpleAuthentication(configuration, "JwtSettings");
                        options.AddSimpleAuthentication(configuration, apiOptionSettings.SimpleAuthenticationSectionName);
                    }

                    // Add Accept-Language header to all endpoints.
                    //options.AddAcceptLanguageHeader();

                    if (apiOptionSettings.AddAcceptLanguageHeader)
                    {
                        // Add Accept-Language header to all endpoints.
                        options.AddAcceptLanguageHeader();
                    }

                    // Add a default (error) response to all endpoints.
                    //options.AddDefaultProblemDetailsResponse();

                    if (apiOptionSettings.AddDefaultProblemDetailsResponse)
                    {
                        // Add a default (error) response to all endpoints.
                        options.AddDefaultProblemDetailsResponse();
                    }

                    // Enable OpenAPI integration for custom parameters.
                    //options.AddOperationParameters();

                    if (apiOptionSettings.AddOperationParameters)
                    {
                        // Enable OpenAPI integration for custom parameters.
                        options.AddOperationParameters();
                    }

                    // Respect the ignored JsonNumberHandling attribute.
                    //options.WriteNumberAsString(); 

                    if (apiOptionSettings.WriteNumberAsString)
                    {
                        // Respect the ignored JsonNumberHandling attribute.
                        options.WriteNumberAsString();
                    }

                    // Describe all query string parameters in Camel Case.
                    //options.DescribeAllParametersInCamelCase();

                    if (apiOptionSettings.DescribeAllParametersInCamelCase)
                    {
                        // Describe all query string parameters in Camel Case.
                        options.DescribeAllParametersInCamelCase();
                    }

                    // Add time examples for TimeSpan and TimeOnly fields.
                    //options.AddTimeExamples();

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
                DefaultApiVersion = new ApiVersion(1),
                ApiVersionReader = new UrlSegmentApiVersionReader(),
                ReportApiVersions = true
            };

            return options;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication MapToolSwaggerUI(AppSettings appSettings, bool routePrefixSetEmpty = false)
        {
            ArgumentNullException.ThrowIfNull(appSettings);

            app.UseSwaggerUI(options =>
            {
                foreach (var version in appSettings.ApiVersions)
                {
                    var url = $"/openapi/{version}.json";
                    options.SwaggerEndpoint(url, $"{app.Environment.ApplicationName} {version}");
                }

                if (routePrefixSetEmpty is true)
                {
                    // Serve the Swagger UI at the app's root (e.g., https://localhost:5001/)
                    options.RoutePrefix = string.Empty;
                }
            });

            return app;
        }

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

                //options.Title = $"{builder.Environment.ApplicationName} API Reference";
                //options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                options.Title = scalarSettings.Title;
                options.WithDefaultHttpClient(scalarSettings.Target, scalarSettings.Client);
            });

            return app;
        }
    }
}