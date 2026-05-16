namespace AppEngine.Tools.Settings;

public class ApiDocsSettings
{
    public string TitleInfoApiVersion { get; set; } = "Route Versioning Web API";
    public string TextInfoApiVersion { get; set; } = "This is the API documentation for the MinimalApi.Template project.";
    public string TextApiDeprecated { get; set; } = " This API version has been deprecated.";
    public bool EnabledExternalDocs { get; set; } = true;
    public string ExternalDocsText { get; set; } = "Find out more about this API";
    public Uri ExternalDocsUri { get; set; } = new Uri("https://www.yourwebsite.com/api-docs");
    public bool EnabledLicense { get; set; } = true;
    public string LicenseText { get; set; } = "MIT License";
    public Uri LicenseUri { get; set; } = new Uri("https://opensource.org/licenses/MIT");
}