using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AppEngine.Tools.SimpleAuthentication.JwtBearer.Settings;

public class JwtBearerSettings
{
    public string SchemeName { get; set; } = JwtBearerDefaults.AuthenticationScheme;
    public string Algorithm { get; set; } = SecurityAlgorithms.HmacSha256;
    public string SecurityKey { get; set; } = null!;
    public string[]? Issuers { get; set; }
    public string[]? Audiences { get; set; }
    public TimeSpan? ExpirationTime { get; set; }
    public TimeSpan ClockSkew { get; set; } = TokenValidationParameters.DefaultClockSkew;
    public string NameClaimType { get; set; } = ClaimsIdentity.DefaultNameClaimType;
    public string RoleClaimType { get; set; } = ClaimsIdentity.DefaultRoleClaimType;
}