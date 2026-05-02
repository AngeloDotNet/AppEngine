using AppEngine.Versioning.Options;
using AppEngine.Versioning.Transformers;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.Settings.Api.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring API versioning and related policies on an IServiceCollection instance.
/// </summary>
/// <remarks>This class contains extension methods that simplify the setup of API versioning, version policies,
/// and OpenAPI documentation for ASP.NET Core applications. It is intended to be used during application startup to
/// ensure consistent versioning behavior across the API.</remarks>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds API versioning and OpenAPI documentation support to the service collection for the specified API versions.
        /// </summary>
        /// <remarks>This method configures both API versioning and the API explorer for versioned APIs, and
        /// registers OpenAPI documentation for each specified version. Use this method to enable versioned API
        /// documentation and policy management in ASP.NET Core applications.</remarks>
        /// <param name="services">The service collection to which API versioning and OpenAPI services will be added.</param>
        /// <param name="apiVersions">An array of strings representing the API version group names to configure and expose in the documentation. Each
        /// entry should correspond to a version identifier, such as "v1" or "v2".</param>
        /// <param name="apiVersioningOptions">Optional. The options used to configure API versioning behavior. If null, default options are applied.</param>
        /// <param name="apiPolicyOptions">Optional. A list of policy options to configure versioning policies, such as sunset dates and deprecation
        /// information, for specific API versions.</param>
        /// <returns>The same IServiceCollection instance so that additional calls can be chained.</returns>
        public IServiceCollection AddVersioningApi(string[] apiVersions, ApiVersioningOptions? apiVersioningOptions = null,
            List<ApiPoliciesOptions>? apiPolicyOptions = null)
        {
            var apiVersionOptions = new ApiVersioningOptions();

            apiVersionOptions = apiVersioningOptions switch
            {
                null => ApiVersionOptions.SetApiVersioningOptions(),
                _ => apiVersioningOptions,
            };

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

            //services.AddApiVersioning(options =>
            //{
            //    options.DefaultApiVersion = new ApiVersion(1);
            //    //options.DefaultApiVersion = new ApiVersion(new DateOnly(2024, 1, 1));
            //    options.ApiVersionReader = new UrlSegmentApiVersionReader();
            //    //options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
            //    //options.AssumeDefaultVersionWhenUnspecified = true;
            //    options.ReportApiVersions = true;

            //options.Policies.Sunset(1)
            //    .Effective(2026, 05, 05)
            //    .Link("https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Policies")
            //    .Title("Version Policies").Type("text/html");
            //})
            services.AddApiVersioning(options => options = apiVersionOptions)
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            //var versions = new string[] { "v1", "v2" };
            var versions = apiVersions;

            foreach (var description in versions)
            {
                services.AddOpenApi(description, options =>
                {
                    options.AddDocumentTransformer<DocumentInfoDocumentTransformer>();
                    options.AddOperationTransformer<ApiVersionDeprecatedTransformer>();
                });
            }

            return services;
        }
    }
}