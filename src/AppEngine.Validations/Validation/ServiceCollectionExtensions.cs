using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Validations.Validation;

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