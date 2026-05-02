using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace AppEngine.Routing;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder ProducesDefaultProblem(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }

    extension(RouteHandlerBuilder builder)
    {
        //public RouteHandlerBuilder WithValidation<TModel>() where TModel : class
        //{
        //    builder.AddEndpointFilter<ValidatorFilter<TModel>>().ProducesValidationProblem();

        //    return builder;
        //}

        public RouteHandlerBuilder WithResponseDescription(int statusCode, string description)
        {
            builder.AddOpenApiOperationTransformer((operation, _, _) =>
            {
                if (operation.Responses?.TryGetValue(statusCode.ToString(), out var response) == true)
                {
                    response.Description = description;
                }

                return Task.CompletedTask;
            });

            return builder;
        }

        public RouteHandlerBuilder WithLocationHeader(string description = "Location of the created resource", int statusCode = StatusCodes.Status201Created)
        {
            builder.AddOpenApiOperationTransformer((operation, _, _) =>
            {
                if (operation.Responses?.TryGetValue(statusCode.ToString(), out var response) == true)
                {
                    if (response is OpenApiResponse openApiResponse && openApiResponse.Headers == null)
                    {
                        openApiResponse.Headers = new Dictionary<string, IOpenApiHeader>();
                    }

                    response.Headers?["Location"] = new OpenApiHeader
                    {
                        Description = description,
                        Required = true,
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Format = "uri"
                        }
                    };
                }

                return Task.CompletedTask;
            });

            return builder;
        }
    }
}