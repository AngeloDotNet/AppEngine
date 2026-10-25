using System.Security.Claims;
using AppEngine.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace AppEngine.Tests.Logging;

public class HttpContextEnricherTests
{
    private sealed class PropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }

    private static LogEvent NewEvent(LogEventLevel level = LogEventLevel.Warning)
        => new(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse("message"), []);

    private static object? Value(LogEvent logEvent, string name)
        => ((ScalarValue)logEvent.Properties[name]).Value;

    [Fact]
    public void Enrich_NoHttpContext_AddsLevelAndIsHttpFalse()
    {
        var logEvent = NewEvent(LogEventLevel.Error);

        new HttpContextEnricher(new HttpContextAccessor()).Enrich(logEvent, new PropertyFactory());

        Assert.Equal((int)LogEventLevel.Error, Value(logEvent, "LevelNumber"));
        Assert.Equal(false, Value(logEvent, "IsHttp"));
        Assert.False(logEvent.Properties.ContainsKey("UserName"));
        Assert.False(logEvent.Properties.ContainsKey("User-Agent"));
    }

    [Fact]
    public void Enrich_WithHttpContext_AddsUserAndUserAgent()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "mario")], "test"))
        };
        context.Request.Headers.UserAgent = "unit-test/1.0";
        var logEvent = NewEvent();

        new HttpContextEnricher(new HttpContextAccessor { HttpContext = context }).Enrich(logEvent, new PropertyFactory());

        Assert.Equal(true, Value(logEvent, "IsHttp"));
        Assert.Equal("mario", Value(logEvent, "UserName"));
        Assert.Equal("unit-test/1.0", Value(logEvent, "User-Agent"));
    }

    [Fact]
    public void Enrich_AnonymousWithoutUserAgent_OmitsBothProperties()
    {
        var logEvent = NewEvent();

        new HttpContextEnricher(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }).Enrich(logEvent, new PropertyFactory());

        Assert.Equal(true, Value(logEvent, "IsHttp"));
        Assert.False(logEvent.Properties.ContainsKey("UserName"));
        Assert.False(logEvent.Properties.ContainsKey("User-Agent"));
    }

    [Fact]
    public void Enrich_DoesNotOverwriteExistingProperties()
    {
        var logEvent = NewEvent();
        logEvent.AddPropertyIfAbsent(new LogEventProperty("IsHttp", new ScalarValue("preset")));

        new HttpContextEnricher(new HttpContextAccessor()).Enrich(logEvent, new PropertyFactory());

        Assert.Equal("preset", Value(logEvent, "IsHttp"));
    }

    [Fact]
    public void Current_Enrich_UserAgentWithControlCharacters_IsStoredVerbatim()
    {
        // Documents current behavior: the header is not sanitized. Structured sinks keep it as data, but plain-text
        // sinks could be exposed to log forging through CR/LF, so it is worth keeping in mind in Phase 6.
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "agent\r\nFAKE LOG LINE";
        var logEvent = NewEvent();

        new HttpContextEnricher(new HttpContextAccessor { HttpContext = context }).Enrich(logEvent, new PropertyFactory());

        Assert.Equal("agent\r\nFAKE LOG LINE", Value(logEvent, "User-Agent"));
    }
}
