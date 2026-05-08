using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools;

public static class ProblemDetailsExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDefaultProblemDetails()
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context => context.UseDefaults();
            });

            return services;
        }
    }

    extension(ProblemDetailsContext context)
    {
        public void UseDefaults()
        {
            var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);

            context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
            context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
            context.ProblemDetails.Detail ??= context.Exception?.Message;
            //context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

            //context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
            context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);

            context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        }
    }
}