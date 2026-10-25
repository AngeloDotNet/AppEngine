using System.Net;
using System.Text.Json;
using AppEngine.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppEngine.Tools.Tests.ExceptionHandlers;

/// <summary>
/// Characterization tests of the current behavior. Tests named "Current_*" document behavior
/// that is planned to change in Phase 6 of PLAN.md (security priority) and must be updated then.
/// </summary>
public class DefaultExceptionHandlerTests
{
    private const string SecretMessage = "Connection string Server=prod;Password=hunter2 failed";

    private static async Task<IHost> StartAsync(string environment, bool useDefaultProblemDetails = false)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .UseEnvironment(environment)
                .ConfigureServices(services =>
                {
                    services.AddDefaultExceptionHandler();
                    if (useDefaultProblemDetails)
                    {
                        services.AddDefaultProblemDetails();
                    }
                })
                .Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.Run(context => context.Request.Path.Value switch
                    {
                        "/boom" => throw new InvalidOperationException(SecretMessage),
                        "/inner" => throw new InvalidOperationException("outer", new TimeoutException("inner-secret")),
                        "/bad" => throw new BadHttpRequestException("bad request body", StatusCodes.Status400BadRequest),
                        _ => context.Response.WriteAsync("ok")
                    });
                }))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(IHost host, string path)
    {
        using var response = await host.GetTestClient().GetAsync(path);
        var json = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, JsonDocument.Parse(json).RootElement.Clone());
    }

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetailsWithStatus500()
    {
        using var host = await StartAsync(Environments.Production);

        var (status, body) = await GetAsync(host, "/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("requestId", out _));
        Assert.Equal("GET /boom", body.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task BadHttpRequestException_ReturnsItsStatusCode()
    {
        using var host = await StartAsync(Environments.Production);

        var (status, body) = await GetAsync(host, "/bad");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Production_DoesNotExposeStackTrace()
    {
        using var host = await StartAsync(Environments.Production);

        var (_, body) = await GetAsync(host, "/boom");

        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Development_ExposesStackTrace()
    {
        using var host = await StartAsync(Environments.Development);

        var (_, body) = await GetAsync(host, "/boom");

        Assert.True(body.TryGetProperty("stackTrace", out var stackTrace));
        Assert.Contains(nameof(DefaultExceptionHandlerTests), stackTrace.GetString() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Current_Production_ExposesExceptionTypeAndMessage()
    {
        // Security finding (PLAN.md, Phase 6 point 6): outside Development the type and message must not reach the client.
        using var host = await StartAsync(Environments.Production);

        var (_, body) = await GetAsync(host, "/boom");

        Assert.Equal(typeof(InvalidOperationException).FullName, body.GetProperty("title").GetString());
        Assert.Equal(SecretMessage, body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Current_Production_ExposesInnerExceptionDetails()
    {
        // Security finding (PLAN.md, Phase 6 point 6): inner exception details leak too.
        using var host = await StartAsync(Environments.Production);

        var (_, body) = await GetAsync(host, "/inner");

        var inner = body.GetProperty("innerException");
        Assert.Equal(typeof(TimeoutException).FullName, inner.GetProperty("title").GetString());
        Assert.Equal("inner-secret", inner.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        using var host = await StartAsync(Environments.Production);

        using var response = await host.GetTestClient().GetAsync("/ok");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DefaultProblemDetails_DoesNotOverwriteHandlerFields()
    {
        using var host = await StartAsync(Environments.Production, useDefaultProblemDetails: true);

        var (status, body) = await GetAsync(host, "/boom");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal(typeof(InvalidOperationException).FullName, body.GetProperty("title").GetString());
        Assert.Equal("GET /boom", body.GetProperty("instance").GetString());
        Assert.True(body.TryGetProperty("traceId", out _) || body.TryGetProperty("requestId", out _));
    }
}
