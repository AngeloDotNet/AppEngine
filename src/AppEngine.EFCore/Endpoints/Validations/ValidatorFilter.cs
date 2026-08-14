using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AppEngine.EFCore.Endpoints.Validations;

internal class ValidatorFilter<TModel>(IValidator<TModel> validator) : IEndpointFilter where TModel : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(a => a?.GetType() == typeof(TModel)) is not TModel input)
        {
            return TypedResults.BadRequest();
        }

        var validationResult = await validator.ValidateAsync(input, context.HttpContext.RequestAborted);

        if (validationResult.IsValid)
        {
            return await next(context);
        }

        var errors = validationResult.ToDictionary();

        var result = TypedResults.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
            instance: context.HttpContext.Request.Path,
            title: "One or more validation errors occurred",
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
                ["errors"] = errors.SelectMany(e => e.Value.Select(m => new { Name = e.Key, Message = m })).ToArray()
            }
        );

        return result;
    }
}