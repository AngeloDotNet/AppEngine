using Scalar.AspNetCore;

namespace AppEngine.Tools.Settings;

public class ScalarSettings
{
    public string Title { get; set; } = string.Empty;
    public bool DarkMode { get; set; } = true;
    public bool ShowSidebar { get; set; } = true;
    public DeveloperToolsVisibility ShowDeveloperToolsVisibility { get; set; } = DeveloperToolsVisibility.Never;
    public ScalarTheme Theme { get; set; } = ScalarTheme.Mars;
    public ScalarTarget Target { get; set; } = ScalarTarget.CSharp;
    public ScalarClient Client { get; set; } = ScalarClient.HttpClient;
}