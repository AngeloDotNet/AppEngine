using AppEngine.Validations.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Validations.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(RouteHandlerBuilder builder)
    {
        /// <summary>
        /// Adds validation to the endpoint using the specified model type. The model will be validated using FluentValidation validators.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to validate.</typeparam>
        /// <returns>The updated <see cref="RouteHandlerBuilder"/> instance.</returns>
        public RouteHandlerBuilder WithValidation<TModel>() where TModel : class
        {
            builder.AddEndpointFilter<ValidatorFilter<TModel>>().ProducesValidationProblem();

            return builder;
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds all FluentValidation validators from the assembly containing the specified validator type to the service collection.
        /// </summary>
        /// <typeparam name="TValidator"></typeparam>
        /// <returns></returns>
        public IServiceCollection AddValidatorsFromAssembly<TValidator>()
        {
            services.AddValidatorsFromAssemblyContaining<TValidator>();
            return services;
        }
    }
}