using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppEngine.Validations.DependencyInjection;
using AppEngine.Validations.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppEngine.Validations.Tests.Validation;

public class WithValidationTests
{
    public sealed record PersonDto(string? Name, int Age);

    public sealed class PersonValidator : AbstractValidator<PersonDto>
    {
        public PersonValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Age).InclusiveBetween(18, 120);
        }
    }

    private static async Task<IHost> StartAsync(Action<IServiceCollection>? configure = null)
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddValidatorsFromAssembly<PersonValidator>();
                    configure?.Invoke(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/people", (PersonDto dto) => Results.Ok(dto.Name)).WithValidation<PersonDto>();

                        // Handler without the validated model: the filter cannot find it in the arguments.
                        endpoints.MapPost("/no-model", () => Results.Ok()).WithValidation<PersonDto>();
                    });
                }))
            .StartAsync();
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task ValidModel_PassesThroughToHandler()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto("Mario", 30));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Mario", await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact]
    public async Task InvalidModel_DefaultFormat_Returns422WithErrorsGroupedByProperty()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto("", 5));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await ReadAsync(response);
        Assert.Equal(422, problem.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred", problem.GetProperty("title").GetString());
        Assert.Equal("/people", problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Age", out _));
    }

    [Fact]
    public async Task InvalidModel_ListFormat_ReturnsFlatArray()
    {
        using var host = await StartAsync(s => s.ConfigureValidation(o => o.ErrorResponseFormat = ErrorResponseFormat.List));

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto("", 5));

        var errors = (await ReadAsync(response)).GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.All(errors.EnumerateArray(), e =>
        {
            Assert.True(e.TryGetProperty("name", out _) || e.TryGetProperty("Name", out _));
            Assert.True(e.TryGetProperty("message", out _) || e.TryGetProperty("Message", out _));
        });
        Assert.True(errors.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task CustomTitleFactory_IsUsed()
    {
        using var host = await StartAsync(s => s.ConfigureValidation(o =>
            o.ValidationErrorTitleMessageFactory = (ctx, errors) => $"{errors.Count} problems on {ctx.HttpContext.Request.Path}"));

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto("", 5));

        Assert.Equal("2 problems on /people", (await ReadAsync(response)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task ModelNotAmongEndpointArguments_Returns400()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().PostAsync("/no-model", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("""{"name":"a","age":"abc"}""")]
    [InlineData("")]
    public async Task MalformedBody_Returns400_NeverServerError(string body)
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().PostAsync("/people", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MaliciousValue_IsRejectedByRulesAndNotReflectedInResponse()
    {
        using var host = await StartAsync();
        const string payload = "<script>alert('xss')</script>";

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto(payload, 30));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("script", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VeryLongValue_IsRejectedWithoutServerError()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().PostAsJsonAsync("/people", new PersonDto(new string('a', 1_000_000), 30));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public void AddValidatorsFromAssembly_RegistersValidators()
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssembly<PersonValidator>();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<PersonValidator>(provider.GetRequiredService<IValidator<PersonDto>>());
    }

    [Fact]
    public void ConfigureValidation_ReturnsSameCollection_AndAppliesOptions()
    {
        var services = new ServiceCollection();

        var returned = services.ConfigureValidation(o => o.ErrorResponseFormat = ErrorResponseFormat.List);

        Assert.Same(services, returned);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(ErrorResponseFormat.List,
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ValidationOptions>>().Value.ErrorResponseFormat);
    }
}
