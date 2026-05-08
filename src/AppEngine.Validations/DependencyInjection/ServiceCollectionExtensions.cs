using AppEngine.Validations.FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AppEngine.Validations.DependencyInjection;

public static class ServiceCollectionExtensions
{
    //extension(IServiceCollection services)
    //{
    //    public IServiceCollection AddHttpJsonOptions()
    //    {
    //        services.ConfigureHttpJsonOptions(options =>
    //        {
    //            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    //            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    //            options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
    //        });

    //        return services;
    //    }

    //    public T? ConfigureAndGet<T>(IConfiguration configuration, string sectionName) where T : class
    //    {
    //        ArgumentNullException.ThrowIfNull(services);
    //        ArgumentNullException.ThrowIfNull(configuration);
    //        ArgumentNullException.ThrowIfNull(sectionName);

    //        var section = configuration.GetSection(sectionName);
    //        services.Configure<T>(section);

    //        var settings = Activator.CreateInstance<T>();
    //        section.Bind(settings);

    //        return settings;
    //    }
    //}

    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder WithValidation<TModel>() where TModel : class
        {
            builder.AddEndpointFilter<ValidatorFilter<TModel>>().ProducesValidationProblem();

            return builder;
        }
    }
}