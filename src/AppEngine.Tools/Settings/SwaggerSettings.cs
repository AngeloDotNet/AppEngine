namespace AppEngine.Tools.Settings;

public class SwaggerSettings
{
    public bool IsEnabled { get; set; }
    public bool RequireAuthentication { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}