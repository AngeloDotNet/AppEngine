using AppEngine.Tools.ExceptionHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools;

public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddDefaultExceptionHandler(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<DefaultExceptionHandler>();

        return services;
    }
}