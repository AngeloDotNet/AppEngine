using AppEngine.Validations.FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
}