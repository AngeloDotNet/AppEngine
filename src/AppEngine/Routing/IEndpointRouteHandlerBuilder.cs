using Microsoft.AspNetCore.Routing;

namespace AppEngine.Routing;

public interface IEndpointRouteHandlerBuilder
{
    static abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
}