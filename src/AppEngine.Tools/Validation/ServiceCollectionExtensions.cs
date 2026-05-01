using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools.Validation;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureValidation(Action<ValidationOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services;
        }
    }
}