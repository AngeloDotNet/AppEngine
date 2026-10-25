using System.Net;
using System.Text.Json;
using AppEngine.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppEngine.Tests.Routing;

public class RoutingTests
{
    public sealed class AlphaEndpoints : IEndpointRouteHandlerBuilder
    {
        public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapGet("/alpha", () => "alpha");
    }

    public sealed class BetaEndpoints : IEndpointRouteHandlerBuilder
    {
        public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapGet("/beta", () => "beta");
    }

    public abstract class AbstractEndpoints : IEndpointRouteHandlerBuilder
    {
        public static void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapGet("/abstract", () => "abstract");
    }

    private static async Task<IHost> StartAsync(Action<IEndpointRouteBuilder> map, bool openApi = false)
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    if (openApi)
                    {
                        services.AddOpenApi();
                    }
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        map(endpoints);
                        if (openApi)
                        {
                            endpoints.MapOpenApi();
                        }
                    });
                }))
            .StartAsync();
    }

    [Fact]
    public async Task MapEndpoints_Generic_MapsSingleType()
    {
        using var host = await StartAsync(e => e.MapEndpoints<AlphaEndpoints>());
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alpha")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/beta")).StatusCode);
    }

    [Fact]
    public async Task MapEndpointsFromAssemblyContaining_WithPredicate_MapsOnlyMatchingTypes()
    {
        using var host = await StartAsync(e => e.MapEndpointsFromAssemblyContaining<RoutingTests>(t => t == typeof(BetaEndpoints)));
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/alpha")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/beta")).StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_Assembly_SkipsAbstractTypes()
    {
        using var host = await StartAsync(e => e.MapEndpoints(typeof(RoutingTests).Assembly));
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alpha")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/beta")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/abstract")).StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_DefaultsToCallingAssembly()
    {
        using var host = await StartAsync(e => e.MapEndpoints(t => t == typeof(AlphaEndpoints)));

        Assert.Equal(HttpStatusCode.OK, (await host.GetTestClient().GetAsync("/alpha")).StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_NullAssembly_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => StartAsync(e => e.MapEndpoints((System.Reflection.Assembly)null!)));
    }

    [Fact]
    public async Task ProducesDefaultProblem_AddsOneMetadataPerStatusCode()
    {
        RouteHandlerBuilder? builder = null;
        using var host = await StartAsync(e => builder = e.MapGet("/x", () => "x").ProducesDefaultProblem(400, 404, 500));

        var endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Single(e => ((RouteEndpoint)e).RoutePattern.RawText == "/x");
        var statusCodes = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(m => m.StatusCode).ToList();

        Assert.NotNull(builder);
        Assert.Contains(400, statusCodes);
        Assert.Contains(404, statusCodes);
        Assert.Contains(500, statusCodes);
    }

    private static async Task<JsonElement> GetOpenApiAsync(IHost host)
    {
        var json = await host.GetTestClient().GetStringAsync("/openapi/v1.json");
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task WithResponseDescription_SetsDescriptionInOpenApiDocument()
    {
        using var host = await StartAsync(e => e.MapGet("/x", () => Results.Ok("x"))
            .WithResponseDescription(200, "Custom ok description"), openApi: true);

        var document = await GetOpenApiAsync(host);

        var description = document.GetProperty("paths").GetProperty("/x").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("description").GetString();
        Assert.Equal("Custom ok description", description);
    }

    [Fact]
    public async Task WithLocationHeader_AddsLocationHeaderToCreatedResponse()
    {
        using var host = await StartAsync(e => e.MapPost("/x", () => Results.Created("/x/1", "x"))
            .Produces<string>(StatusCodes.Status201Created)
            .WithLocationHeader("Where it lives"), openApi: true);

        var document = await GetOpenApiAsync(host);

        var location = document.GetProperty("paths").GetProperty("/x").GetProperty("post")
            .GetProperty("responses").GetProperty("201").GetProperty("headers").GetProperty("Location");
        Assert.Equal("Where it lives", location.GetProperty("description").GetString());
        Assert.True(location.GetProperty("required").GetBoolean());
        Assert.Equal("uri", location.GetProperty("schema").GetProperty("format").GetString());
    }
}
