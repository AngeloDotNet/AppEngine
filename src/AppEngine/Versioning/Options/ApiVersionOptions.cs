using Asp.Versioning;

namespace AppEngine.Versioning.Options;

/// <summary>
/// Provides helper methods for configuring API versioning options in an ASP.NET Core application.
/// </summary>
/// <remarks>This static class is intended to centralize the setup of API versioning configuration, making it
/// easier to apply consistent versioning policies across the application. It is typically used during application
/// startup to configure API versioning services.</remarks>
public static class ApiVersionOptions
{
    /// <summary>
    /// Configures and returns a new instance of ApiVersioningOptions with default settings for API versioning.
    /// </summary>
    /// <remarks>The returned options use URL segments to read the API version and enable reporting of
    /// supported API versions in responses. The default API version is set to 1. Adjust the returned options as needed
    /// to fit specific versioning requirements.</remarks>
    /// <returns>A configured ApiVersioningOptions instance with default API version set to 1, URL segment version reader, and
    /// API version reporting enabled.</returns>
    public static ApiVersioningOptions SetApiVersioningOptions()
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