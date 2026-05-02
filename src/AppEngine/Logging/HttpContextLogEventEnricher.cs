using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace AppEngine.Logging;

public class HttpContextEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LevelNumber", (int)logEvent.Level));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IsHttp", httpContext is not null));

        if (httpContext is null)
        {
            return;
        }

        var userName = httpContext.User?.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", userName));
        }

        if (httpContext.Request?.Headers?.TryGetValue("User-Agent", out var userAgent) == true)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("User-Agent", userAgent.ToString()));
        }
    }
}