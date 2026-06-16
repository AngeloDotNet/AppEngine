namespace AppEngine.Tools.Settings;

public class SwaggerSettings
{
    public bool RequireAuthentication { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool UseKeyCloakAuth { get; set; } = false;
    public string KeyCloakClientId { get; set; } = null!;
}