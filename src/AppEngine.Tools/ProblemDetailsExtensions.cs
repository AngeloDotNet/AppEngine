using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddDefaultProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context => context.UseDefaults();
        });

        return services;
    }

    public static void UseDefaults(this ProblemDetailsContext context)
    {
        var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);

        context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
        context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Detail ??= context.Exception?.Message;

        context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
    }
}