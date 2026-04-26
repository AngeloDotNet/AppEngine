using Microsoft.AspNetCore.Http;

namespace AppEngine.Tools.OperationResults.AspNetCore.Http;

public static class HttpContextExtensions
{
    extension(HttpContext httpContext)
    {
        public IResult CreateResponse(Result result, int? successStatusCode = null)
            => result.ToResponse(httpContext, successStatusCode);

        public IResult CreateResponse(Result result, string? routeName, object? routeValues = null)
            => result.ToResponse(httpContext, routeName, routeValues);

        public IResult CreateResponse<T>(Result<T> result, int? successStatusCode = null)
            => result.ToResponse(httpContext, null, null, successStatusCode);

        public IResult CreateResponse<T>(Result<T> result, string? routeName, object? routeValues = null, int? successStatusCode = null)
            => result.ToResponse(httpContext, routeName, routeValues, successStatusCode);
    }
}