using System.Text;
using System.Text.Json;
using AppEngine.Tools.OperationResults;
using AppEngine.Tools.OperationResults.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools.Tests.OperationResults;

public class OperationResultExtensionsTests
{
    private sealed record Person(int Id, string Name);

    private sealed record Response(int Status, string ContentType, string Body)
    {
        public JsonElement Json => JsonDocument.Parse(Body).RootElement;
    }

    private static DefaultHttpContext CreateContext(OperationResultOptions? options = null, string method = "GET", string path = "/people/1")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        if (options is not null)
        {
            services.AddSingleton(options);
        }

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<Response> ExecuteAsync(IResult result, DefaultHttpContext context)
    {
        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return new(context.Response.StatusCode, context.Response.ContentType ?? string.Empty, await reader.ReadToEndAsync());
    }

    // Result (non generic)

    [Fact]
    public async Task Result_Ok_Returns204ByDefault()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Ok().ToResponse(context), context);

        Assert.Equal(StatusCodes.Status204NoContent, response.Status);
        Assert.Equal(string.Empty, response.Body);
    }

    [Fact]
    public async Task Result_Ok_UsesCustomSuccessStatusCode()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Ok().ToResponse(context, StatusCodes.Status202Accepted), context);

        Assert.Equal(StatusCodes.Status202Accepted, response.Status);
    }

    [Theory]
    [InlineData(FailureReasons.ClientError, 400)]
    [InlineData(FailureReasons.Unauthorized, 401)]
    [InlineData(FailureReasons.Forbidden, 403)]
    [InlineData(FailureReasons.ItemNotFound, 404)]
    [InlineData(FailureReasons.InvalidRequest, 406)]
    [InlineData(FailureReasons.Timeout, 408)]
    [InlineData(FailureReasons.Conflict, 409)]
    [InlineData(FailureReasons.InvalidFile, 415)]
    [InlineData(FailureReasons.InvalidContent, 422)]
    [InlineData(FailureReasons.DatabaseError, 500)]
    [InlineData(FailureReasons.NetworkError, 500)]
    [InlineData(FailureReasons.ServiceUnavailable, 503)]
    [InlineData(FailureReasons.GenericError, 500)]
    public async Task Result_Fail_MapsFailureReasonToHttpStatus(int failureReason, int expectedStatus)
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Fail(failureReason).ToResponse(context), context);

        Assert.Equal(expectedStatus, response.Status);
        Assert.Equal(expectedStatus, response.Json.GetProperty("status").GetInt32());
        // The "type" is filled by ASP.NET Core's ProblemDetails defaults (an RFC URL), so the
        // "https://httpstatuses.io/{status}" fallback in OperationResultExtensions.Problem is currently never used.
        Assert.Contains("rfc", response.Json.GetProperty("type").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Result_Fail_BuildsProblemDetails()
    {
        var context = CreateContext(method: "POST", path: "/people");

        var response = await ExecuteAsync(
            Result.Fail(FailureReasons.ItemNotFound, "Person not found", "Id 42").ToResponse(context), context);

        Assert.Contains("problem+json", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("Person not found", response.Json.GetProperty("title").GetString());
        Assert.Equal("Id 42", response.Json.GetProperty("detail").GetString());
        Assert.Equal("POST /people", response.Json.GetProperty("instance").GetString());
        Assert.True(response.Json.TryGetProperty("requestId", out _));
    }

    [Fact]
    public async Task Result_Fail_WithoutMessage_UsesReasonPhraseAsTitle()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Fail(FailureReasons.Conflict).ToResponse(context), context);

        Assert.Equal("Conflict", response.Json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Current_Result_Fail_WithException_ExposesExceptionMessageToClient()
    {
        // Security finding (to add to PLAN.md Phase 6): Result.Fail(reason, exception) copies the exception message
        // (and the inner exception message) into the HTTP response, whatever the environment.
        var context = CreateContext();
        var error = new InvalidOperationException("Server=prod;Password=hunter2", new TimeoutException("inner-secret"));

        var response = await ExecuteAsync(Result.Fail(FailureReasons.GenericError, error).ToResponse(context), context);

        Assert.Equal("Server=prod;Password=hunter2", response.Json.GetProperty("title").GetString());
        Assert.Equal("inner-secret", response.Json.GetProperty("detail").GetString());
    }

    // Failure reason mapping options

    [Fact]
    public async Task UnmappedFailureReason_DefaultBehavior_Returns501()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Fail(9999).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status501NotImplemented, response.Status);
    }

    [Fact]
    public async Task UnmappedFailureReason_UseFailureReason_UsesItAsStatusCode()
    {
        var options = new OperationResultOptions { UnmappedFailureReasonBehavior = UnmappedFailureReasonBehavior.UseFailureReason };
        var context = CreateContext(options);

        var response = await ExecuteAsync(Result.Fail(418).ToResponse(context), context);

        Assert.Equal(418, response.Status);
    }

    [Fact]
    public async Task MapStatusCodesDisabled_UsesFailureReasonAsStatusCode()
    {
        var options = new OperationResultOptions { MapStatusCodes = false };
        var context = CreateContext(options);

        var response = await ExecuteAsync(Result.Fail(StatusCodes.Status409Conflict).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status409Conflict, response.Status);
    }

    [Fact]
    public async Task CustomStatusCodeMapping_IsUsed()
    {
        var options = new OperationResultOptions();
        options.StatusCodesMapping[FailureReasons.ItemNotFound] = StatusCodes.Status410Gone;
        var context = CreateContext(options);

        var response = await ExecuteAsync(Result.Fail(FailureReasons.ItemNotFound).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status410Gone, response.Status);
    }

    // Validation errors

    private static readonly ValidationError[] Errors =
    [
        new("Name", "Name is required"),
        new("Name", "Name is too short"),
        new("Age", "Age must be positive")
    ];

    [Fact]
    public async Task ValidationErrors_DefaultFormat_AreGroupedByName()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Fail(FailureReasons.ClientError, Errors).ToResponse(context), context);

        var errors = response.Json.GetProperty("errors");
        Assert.Equal(2, errors.GetProperty("Name").GetArrayLength());
        Assert.Equal("Age must be positive", errors.GetProperty("Age")[0].GetString());
    }

    [Fact]
    public async Task ValidationErrors_ListFormat_AreReturnedAsList()
    {
        var context = CreateContext(new OperationResultOptions { ErrorResponseFormat = ErrorResponseFormat.List });

        var response = await ExecuteAsync(Result.Fail(FailureReasons.ClientError, Errors).ToResponse(context), context);

        var errors = response.Json.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(3, errors.GetArrayLength());
    }

    [Fact]
    public async Task NoValidationErrors_DoNotAddErrorsProperty()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result.Fail(FailureReasons.ClientError).ToResponse(context), context);

        Assert.False(response.Json.TryGetProperty("errors", out _));
    }

    // Result<T>

    [Fact]
    public async Task ResultOfT_Ok_ReturnsJsonContentWith200()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result<Person>.Ok(new Person(1, "Mario")).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Contains("application/json", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("Mario", response.Json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ResultOfT_Ok_CustomSuccessStatusCode()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result<Person>.Ok(new Person(1, "Mario")).ToResponse(context, StatusCodes.Status202Accepted), context);

        Assert.Equal(StatusCodes.Status202Accepted, response.Status);
    }

    [Fact]
    public async Task ResultOfT_Ok_WithoutContent_Returns204()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result<Person>.Ok().ToResponse(context), context);

        Assert.Equal(StatusCodes.Status204NoContent, response.Status);
    }

    [Fact]
    public async Task ResultOfT_Fail_ReturnsProblemDetails()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(Result<Person>.Fail(FailureReasons.ItemNotFound, "nope").ToResponse(context), context);

        Assert.Equal(StatusCodes.Status404NotFound, response.Status);
        Assert.Equal("nope", response.Json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ResultOfT_Fail_WithContent_ReturnsContentInsteadOfProblemDetails()
    {
        var context = CreateContext();

        var response = await ExecuteAsync(
            Result<Person>.Fail(FailureReasons.Conflict, new Person(7, "Existing"), (IEnumerable<ValidationError>?)null).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status409Conflict, response.Status);
        Assert.Equal(7, response.Json.GetProperty("id").GetInt32());
        Assert.False(response.Json.TryGetProperty("type", out _));
    }

    [Fact]
    public async Task ResultOfT_Ok_ByteArrayFileContent_ReturnsFile()
    {
        var context = CreateContext();
        var content = new ByteArrayFileContent([1, 2, 3], "application/octet-stream", "data.bin");

        var response = await ExecuteAsync(Result<ByteArrayFileContent>.Ok(content).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal("application/octet-stream", response.ContentType);
        Assert.Contains("data.bin", context.Response.Headers.ContentDisposition.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResultOfT_Ok_StreamFileContent_ReturnsFileStream()
    {
        var context = CreateContext();
        var content = new StreamFileContent(new MemoryStream("hello"u8.ToArray()), "text/plain", "hello.txt");

        var response = await ExecuteAsync(Result<StreamFileContent>.Ok(content).ToResponse(context), context);

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal("text/plain", response.ContentType);
        Assert.Equal("hello", response.Body);
    }

    // Misc

    [Fact]
    public void Result_ImplicitBoolConversion_ReflectsSuccess()
    {
        bool ok = Result.Ok();
        bool failed = Result.Fail(FailureReasons.GenericError);

        Assert.True(ok);
        Assert.False(failed);
    }

    [Fact]
    public void ResultOfT_ImplicitConversionFromValue_IsSuccessful()
    {
        Result<Person> result = new Person(1, "Mario");

        Assert.True(result.Success);
        Assert.True(result.TryGetContent(out var person));
        Assert.Equal("Mario", person.Name);
    }

    [Fact]
    public void ResultOfT_ImplicitConversionFromFailedResult_KeepsFailureData()
    {
        Result<Person> result = Result.Fail(FailureReasons.Forbidden, "denied");

        Assert.False(result.Success);
        Assert.Equal(FailureReasons.Forbidden, result.FailureReason);
        Assert.Equal("denied", result.ErrorMessage);
    }
}
