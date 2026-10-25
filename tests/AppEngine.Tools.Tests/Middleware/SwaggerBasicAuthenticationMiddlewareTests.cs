using System.Text;
using AppEngine.Tools.Middleware;
using AppEngine.Tools.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AppEngine.Tools.Tests.Middleware;

/// <summary>
/// Characterization tests of the current behavior. Tests named "Current_*" document behavior
/// that is planned to change in Phase 6 of PLAN.md (security priority) and must be updated then.
/// </summary>
public class SwaggerBasicAuthenticationMiddlewareTests
{
    private const string UserName = "admin";
    private const string Password = "s3cret:with:colons";

    private static (SwaggerBasicAuthenticationMiddleware Middleware, Func<bool> NextCalled) Create(
        string? userName = UserName, string? password = Password)
    {
        var nextCalled = false;
        var middleware = new SwaggerBasicAuthenticationMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            Options.Create(new SwaggerSettings { UserName = userName, Password = password }));
        return (middleware, () => nextCalled);
    }

    private static DefaultHttpContext CreateContext(string path, string? authorization = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        return context;
    }

    private static string Basic(string userName, string password) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));

    [Theory]
    [InlineData("/api/orders")]
    [InlineData("/swaggerx")]
    [InlineData("/")]
    public async Task Path_NotProtected_CallsNextWithoutCredentials(string path)
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext(path);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled());
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/index.html")]
    [InlineData("/swagger")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Path_Protected_WithoutHeader_Returns401WithChallenge(string path)
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext(path);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Basic", context.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public async Task Path_Protected_ValidCredentials_CallsNext()
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext("/swagger", Basic(UserName, Password));

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled());
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("admin", "wrong")]
    [InlineData("wrong", "s3cret:with:colons")]
    [InlineData("ADMIN", "s3cret:with:colons")]
    [InlineData("", "")]
    [InlineData("admin", "s3cret:with:colons ")]
    public async Task Path_Protected_WrongCredentials_Returns401(string userName, string password)
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext("/swagger", Basic(userName, password));

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task Path_Protected_NonBasicScheme_Returns401()
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext("/swagger", "Bearer abc.def.ghi");

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task Path_Protected_CredentialsWithoutColon_Returns401()
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext("/swagger", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("adminonly")));

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("admin", null)]
    [InlineData(null, "secret")]
    [InlineData("", "")]
    public async Task Settings_WithoutCompleteCredentials_DoNotProtectPaths(string? userName, string? password)
    {
        var (middleware, nextCalled) = Create(userName, password);
        var context = CreateContext("/swagger");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled());
    }

    [Fact]
    public async Task Current_MalformedBase64_ThrowsFormatException_InsteadOfReturning401()
    {
        // Security finding (PLAN.md, Phase 6 point 5): a malformed header must produce 401, not an unhandled exception (500).
        var (middleware, nextCalled) = Create();
        var context = CreateContext("/swagger", "Basic !!!not-base64!!!");

        await Assert.ThrowsAsync<FormatException>(() => middleware.InvokeAsync(context));

        Assert.False(nextCalled());
    }

    [Theory]
    [InlineData("/SWAGGER")]
    [InlineData("/Swagger/v1/swagger.json")]
    [InlineData("/Index.HTML")]
    [InlineData("/swagger/../admin")]
    public async Task Path_CaseVariationOrTraversal_DoesNotBypassAuthentication(string path)
    {
        var (middleware, nextCalled) = Create();
        var context = CreateContext(path);

        await middleware.InvokeAsync(context);

        // Path matching is case-insensitive; a path still starting with the /swagger segment stays protected.
        Assert.False(nextCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
