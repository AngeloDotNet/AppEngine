using System.Security.Claims;

namespace AppEngine.Tools.SimpleAuthentication.JwtBearer.Interfaces;

public interface IJwtBearerService
{
    Task<string> CreateTokenAsync(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null);
    Task<ClaimsPrincipal> ValidateTokenAsync(string token, bool validateLifetime = true);

    async Task<JwtBearerValidationResult> TryValidateTokenAsync(string token, bool validateLifetime = true)
    {
        var result = new JwtBearerValidationResult();

        try
        {
            var principal = await ValidateTokenAsync(token, validateLifetime);
            result = new JwtBearerValidationResult { IsValid = true, Principal = principal };
        }
        catch (Exception ex)
        {
            result = new JwtBearerValidationResult { IsValid = false, Exception = ex };
        }

        return result;
    }

    Task<string> RefreshTokenAsync(string token, DateTime? absoluteExpiration = null)
        => RefreshTokenAsync(token, true, absoluteExpiration);

    Task<string> RefreshTokenAsync(string token, bool validateLifetime, DateTime? absoluteExpiration = null);
}