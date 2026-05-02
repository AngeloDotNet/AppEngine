namespace AppEngine.Versioning.Options;

/// <summary>
/// Represents configuration options for describing API versioning policies in metadata.
/// </summary>
/// <remarks>This class is typically used to provide information about API versioning policies, such as version
/// number, effective date, and related documentation links, for use in API metadata or documentation generation
/// scenarios.</remarks>
public class ApiPoliciesOptions
{
    /// <summary>
    /// Gets or sets the major version number of the application or component.
    /// </summary>
    public int MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the associated change or action becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets the hyperlink associated with the item.
    /// </summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title associated with the resource.
    /// </summary>
    public string Title { get; set; } = "https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Policies";

    /// <summary>
    /// Gets or sets the media type of the content.
    /// </summary>
    /// <remarks>The media type is typically specified as a MIME type, such as "text/html" or
    /// "application/json". Setting this property determines how the content is interpreted by consumers.</remarks>
    public string Type { get; set; } = "text/html";
}