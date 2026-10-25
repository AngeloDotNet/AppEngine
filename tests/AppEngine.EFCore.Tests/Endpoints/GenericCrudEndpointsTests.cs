using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppEngine.EFCore.DependencyInjection;
using AppEngine.EFCore.Endpoints;
using AppEngine.EFCore.Tests.TestSupport;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppEngine.EFCore.Tests.Endpoints;

public class GenericCrudEndpointsTests : IAsyncLifetime
{
    public sealed record TagReadDto(int Id, string Label);

    public sealed record TagCreateDto(string? Label);

    public sealed record TagUpdateDto(string? Label);

    private sealed class CreateValidator : AbstractValidator<TagCreateDto>
    {
        public CreateValidator() => RuleFor(x => x.Label).NotEmpty().MaximumLength(20);
    }

    private sealed class UpdateValidator : AbstractValidator<TagUpdateDto>
    {
        public UpdateValidator() => RuleFor(x => x.Label).NotEmpty().MaximumLength(20);
    }

    private readonly TestDatabase db = new(withInterceptors: false);
    private IHost host = null!;
    private HttpClient client = null!;

    public Task InitializeAsync() => InitializeCoreAsync();

    public Task DisposeAsync()
    {
        client.Dispose();
        host.Dispose();
        db.Dispose();
        return Task.CompletedTask;
    }

    private async Task InitializeCoreAsync()
    {
        host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddScoped(_ => db.CreateContext());
                    services.AddPersistence<Tag, int, TestDbContext>();
                    services.AddScoped<IValidator<TagCreateDto>, CreateValidator>();
                    services.AddScoped<IValidator<TagUpdateDto>, UpdateValidator>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapCrudEndpoints<Tag, int, TagReadDto, TagCreateDto, TagUpdateDto>(
                        "/tags", "Tags", "Tags",
                        e => e.Id,
                        e => new TagReadDto(e.Id, e.Label),
                        d => new Tag { Label = d.Label ?? string.Empty },
                        (e, d) => e.Label = d.Label ?? string.Empty));
                }))
            .StartAsync();

        client = host.GetTestClient();
    }

    private async Task<int> SeedAsync(string label)
    {
        await using var context = db.CreateContext();
        var tag = new Tag { Label = label };
        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        return tag.Id;
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task GetAll_ReturnsAllItems()
    {
        await SeedAsync("a");
        await SeedAsync("b");

        var response = await client.GetAsync("/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<TagReadDto>>();
        Assert.Equal(["a", "b"], items!.Select(i => i.Label));
    }

    [Fact]
    public async Task GetById_ExistingAndMissing()
    {
        var id = await SeedAsync("a");

        var found = await client.GetAsync($"/tags/{id}");
        var missing = await client.GetAsync("/tags/999");

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal("a", (await found.Content.ReadFromJsonAsync<TagReadDto>())!.Label);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1%20OR%201=1")]
    [InlineData("99999999999999999999")]
    [InlineData("..%2f..%2fetc%2fpasswd")]
    public async Task GetById_InvalidId_IsBadRequestOrNotFound_NeverServerError(string id)
    {
        var response = await client.GetAsync($"/tags/{id}");

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound, response.StatusCode.ToString());
    }

    [Fact]
    public async Task Create_Valid_Returns201WithLocationAndBody()
    {
        var response = await client.PostAsJsonAsync("/tags", new TagCreateDto("new"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TagReadDto>();
        Assert.Equal("new", dto!.Label);
        Assert.Equal($"/tags/{dto.Id}", response.Headers.Location!.OriginalString);

        await using var verify = db.CreateContext();
        Assert.Single(verify.Tags);
    }

    [Theory]
    [InlineData("""{"label":""}""")]
    [InlineData("""{"label":null}""")]
    [InlineData("""{}""")]
    [InlineData("""{"label":"this label is definitely longer than twenty characters"}""")]
    public async Task Create_Invalid_Returns422WithErrorsAndNothingSaved(string body)
    {
        var response = await client.PostAsync("/tags", Json(body));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(422, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.GetProperty("errors").GetArrayLength() > 0);
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));

        await using var verify = db.CreateContext();
        Assert.Empty(verify.Tags);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("""{"label": 123}""")]
    public async Task Create_MalformedBody_Returns400(string body)
    {
        var response = await client.PostAsync("/tags", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WrongContentType_Returns415()
    {
        var response = await client.PostAsync("/tags", new StringContent("""{"label":"a"}""", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("';DROP TABLE Tags;--")]
    [InlineData("<script>x</script>")]
    [InlineData("' OR '1'='1")]
    public async Task Create_MaliciousLabel_IsStoredAsPlainData(string label)
    {
        var response = await client.PostAsJsonAsync("/tags", new TagCreateDto(label));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var verify = db.CreateContext();
        Assert.Equal(label, verify.Tags.Single().Label);
    }

    [Fact]
    public async Task Update_Existing_Returns204AndPersists()
    {
        var id = await SeedAsync("old");

        var response = await client.PutAsJsonAsync($"/tags/{id}", new TagUpdateDto("new"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var verify = db.CreateContext();
        Assert.Equal("new", verify.Tags.Single().Label);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var response = await client.PutAsJsonAsync("/tags/999", new TagUpdateDto("new"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Invalid_Returns422AndKeepsOldValue()
    {
        var id = await SeedAsync("old");

        var response = await client.PutAsJsonAsync($"/tags/{id}", new TagUpdateDto(""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await using var verify = db.CreateContext();
        Assert.Equal("old", verify.Tags.Single().Label);
    }

    [Fact]
    public async Task Delete_ExistingAndMissing()
    {
        var id = await SeedAsync("a");

        var deleted = await client.DeleteAsync($"/tags/{id}");
        var again = await client.DeleteAsync($"/tags/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        await using var verify = db.CreateContext();
        Assert.Empty(verify.Tags);
    }

    [Fact]
    public async Task Current_GetAll_HasNoPagination()
    {
        // Documents current behavior: GET / returns every row, with no paging or limit.
        for (var i = 0; i < 150; i++)
        {
            await using var context = db.CreateContext();
            context.Tags.Add(new Tag { Label = $"t{i}" });
            await context.SaveChangesAsync();
        }

        var items = await client.GetFromJsonAsync<List<TagReadDto>>("/tags");

        Assert.Equal(150, items!.Count);
    }
}
