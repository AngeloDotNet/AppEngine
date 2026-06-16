using AppEngine.Tools.Enums;

namespace AppEngine.Tools.Settings;

public class AppSettings
{
    public string[] ApiVersions { get; init; } = [];
    public string[] SupportedCultures { get; init; } = [];
    public ApiDocumentationTool ApiDocumentationTool { get; init; } = ApiDocumentationTool.SwaggerUI;
}