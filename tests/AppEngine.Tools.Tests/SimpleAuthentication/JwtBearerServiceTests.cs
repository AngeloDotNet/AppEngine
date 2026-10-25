using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AppEngine.Tools.SimpleAuthentication.Abstractions;
using AppEngine.Tools.SimpleAuthentication.JwtBearer;
using AppEngine.Tools.SimpleAuthentication.JwtBearer.Settings;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AppEngine.Tools.Tests.SimpleAuthentication;

public class JwtBearerServiceTests
{
    private const string Key = "unit-test-signing-key-with-at-least-32-bytes!!";
    private const string LongKey = "unit-test-signing-key-with-at-least-64-bytes-for-HS512-algorithm-0123456789";

    private static JwtBearerService CreateService(Action<JwtBearerSettings>? configure = null)
    {
        var settings = new JwtBearerSettings { SecurityKey = Key, ClockSkew = TimeSpan.Zero };
        configure?.Invoke(settings);
        return new JwtBearerService(Options.Create(settings));
    }

    private static string Base64Url(string json) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));

    [Fact]
    public void Constructor_NullOptions_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new JwtBearerService(null!));

    [Fact]
    public async Task CreateToken_ThenValidate_RoundTripsUserNameAndClaims()
    {
        var service = CreateService();
        IList<Claim> claims = [new Claim("tenant", "acme"), new Claim(ClaimTypes.Role, "admin")];

        var token = await service.CreateTokenAsync("mario", claims);
        var principal = await service.ValidateTokenAsync(token);

        Assert.Equal("mario", principal.Identity!.Name);
        Assert.Equal("acme", principal.FindFirst("tenant")?.Value);
        Assert.True(principal.IsInRole("admin"));
        Assert.False(string.IsNullOrEmpty(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
    }

    [Fact]
    public async Task CreateToken_EachCallHasDifferentJti()
    {
        var service = CreateService();

        var first = await service.ValidateTokenAsync(await service.CreateTokenAsync("mario"));
        var second = await service.ValidateTokenAsync(await service.CreateTokenAsync("mario"));

        Assert.NotEqual(
            first.FindFirst(JwtRegisteredClaimNames.Jti)?.Value,
            second.FindFirst(JwtRegisteredClaimNames.Jti)?.Value);
    }

    [Fact]
    public async Task CreateToken_ExpirationInThePast_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateTokenAsync("mario", absoluteExpiration: DateTime.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public async Task CreateToken_UsesFirstConfiguredIssuerAndAudience()
    {
        var service = CreateService(s => { s.Issuers = ["issuer-a", "issuer-b"]; s.Audiences = ["aud-a"]; });

        var principal = await service.ValidateTokenAsync(await service.CreateTokenAsync("mario"));

        Assert.Equal("issuer-a", principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value);
        Assert.Equal("aud-a", principal.FindFirst(JwtRegisteredClaimNames.Aud)?.Value);
    }

    [Fact]
    public async Task ValidateToken_NotAJwt_ThrowsSecurityTokenException()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<SecurityTokenException>(() => service.ValidateTokenAsync("not-a-jwt"));

        Assert.Contains("well formed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a.b")]
    [InlineData("..")]
    [InlineData("' OR 1=1 --")]
    [InlineData("<script>alert(1)</script>")]
    public async Task ValidateToken_MalformedOrMaliciousInput_ThrowsSecurityTokenException(string token)
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<SecurityTokenException>(() => service.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_TamperedPayload_IsRejected()
    {
        var service = CreateService();
        var token = await service.CreateTokenAsync("mario");
        var parts = token.Split('.');
        var forgedPayload = Base64Url("""{"unique_name":"admin","exp":4102444800}""");

        var forged = $"{parts[0]}.{forgedPayload}.{parts[2]}";

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.ValidateTokenAsync(forged));
    }

    [Fact]
    public async Task ValidateToken_SignedWithDifferentKey_IsRejected()
    {
        var attacker = CreateService(s => s.SecurityKey = "another-attacker-key-with-at-least-32-bytes!!!");
        var service = CreateService();

        var token = await attacker.CreateTokenAsync("mario");

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_AlgNone_IsRejected()
    {
        var service = CreateService();
        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var payload = Base64Url("""{"unique_name":"admin","exp":4102444800}""");

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.ValidateTokenAsync($"{header}.{payload}."));
    }

    [Fact]
    public async Task ValidateToken_DifferentAlgorithmThanConfigured_IsRejected()
    {
        var hs512 = CreateService(s => { s.SecurityKey = LongKey; s.Algorithm = SecurityAlgorithms.HmacSha512; });
        var hs256 = CreateService(s => s.SecurityKey = LongKey);

        var token = await hs512.CreateTokenAsync("mario");

        await Assert.ThrowsAsync<SecurityTokenException>(() => hs256.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_Expired_RejectedUnlessLifetimeValidationDisabled()
    {
        var service = CreateService();
        var token = await service.CreateTokenAsync("mario", absoluteExpiration: DateTime.UtcNow.AddMilliseconds(300));
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.ValidateTokenAsync(token));

        var principal = await service.ValidateTokenAsync(token, validateLifetime: false);
        Assert.Equal("mario", principal.Identity!.Name);
    }

    [Fact]
    public async Task ValidateToken_WrongIssuer_IsRejected()
    {
        var issuer = CreateService(s => s.Issuers = ["issuer-b"]);
        var validator = CreateService(s => s.Issuers = ["issuer-a"]);

        var token = await issuer.CreateTokenAsync("mario");

        await Assert.ThrowsAsync<SecurityTokenException>(() => validator.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_WrongAudience_IsRejected()
    {
        var issuer = CreateService(s => s.Audiences = ["aud-b"]);
        var validator = CreateService(s => s.Audiences = ["aud-a"]);

        var token = await issuer.CreateTokenAsync("mario");

        await Assert.ThrowsAsync<SecurityTokenException>(() => validator.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateToken_NoIssuersConfigured_DoesNotValidateIssuer()
    {
        var issuer = CreateService();
        var token = await issuer.CreateTokenAsync("mario", issuer: "anything");
        var validator = CreateService();

        var principal = await validator.ValidateTokenAsync(token);

        Assert.Equal("mario", principal.Identity!.Name);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokenForSameUser()
    {
        var service = CreateService(s => s.ExpirationTime = TimeSpan.FromMinutes(5));
        var original = await service.CreateTokenAsync("mario", [new Claim("tenant", "acme")]);

        var refreshed = await service.RefreshTokenAsync(original, validateLifetime: true);

        Assert.NotEqual(original, refreshed);
        var principal = await service.ValidateTokenAsync(refreshed);
        Assert.Equal("mario", principal.Identity!.Name);
        Assert.Equal("acme", principal.FindFirst("tenant")?.Value);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ThrowsSecurityTokenException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.RefreshTokenAsync("garbage", validateLifetime: true));
    }
}
