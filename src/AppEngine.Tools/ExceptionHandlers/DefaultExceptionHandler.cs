using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace AppEngine.Tools.ExceptionHandlers;

internal class DefaultExceptionHandler(IProblemDetailsService problemDetailsService, IWebHostEnvironment webHostEnvironment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException badHttpRequestException)
        {
            httpContext.Response.StatusCode = badHttpRequestException.StatusCode;
        }

        var problemDetails = new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = exception.GetType().FullName,
            Detail = exception.Message,
            //Instance = httpContext.Request.Path
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        //problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        problemDetails.Extensions.TryAdd("traceId", activity?.Id);

        problemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);

        if (exception.InnerException is not null)
        {
            problemDetails.Extensions["innerException"] = new
            {
                Title = exception.InnerException.GetType().FullName,
                Detail = exception.InnerException.Message
            };
        }

        if (webHostEnvironment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        await problemDetailsService.WriteAsync(new()
        {
            HttpContext = httpContext,
            AdditionalMetadata = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint?.Metadata,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}