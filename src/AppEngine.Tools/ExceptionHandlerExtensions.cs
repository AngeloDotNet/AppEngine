using AppEngine.Tools.ExceptionHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools;

public static class ExceptionHandlerExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDefaultExceptionHandler()
        {
            services.AddProblemDetails();
            services.AddExceptionHandler<DefaultExceptionHandler>();

            return services;
        }
    }
}