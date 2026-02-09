using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AppEngine.FluentValidation;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithValidation<TModel>(this RouteHandlerBuilder builder) where TModel : class
    {
        builder.AddEndpointFilter<ValidatorFilter<TModel>>().ProducesValidationProblem();

        return builder;
    }
}