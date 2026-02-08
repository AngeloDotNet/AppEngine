using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Validation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureValidation(this IServiceCollection services, Action<ValidationOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }
}