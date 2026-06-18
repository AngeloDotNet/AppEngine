using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace AppEngine.Tools.Settings;

public class KeyCloakSettings
{
    public string Name { get; set; } = nameof(SecuritySchemeType.OAuth2);
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AuthorizationUrl => $"{BaseUrl}/realms/{Realm}/protocol/openid-connect/auth";
    public string TokenUrl => $"{BaseUrl}/realms/{Realm}/protocol/openid-connect/token";
    public Dictionary<string, string> Scopes { get; set; } = [];

    public static KeyCloakSettings AddOpenApiKeyCloakSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new KeyCloakSettings
        {
            Name = configuration["Keycloak:Name"],
            BaseUrl = configuration["Keycloak:BaseUrl"],
            Realm = configuration["Keycloak:Realm"],
            Scopes = configuration.GetSection("Keycloak:Scopes").Get<Dictionary<string, string>>()
        };
    }
}