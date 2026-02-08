using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace AppEngine.Routing;

public static class IEndpointRouteBuilderExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder endpoints, Func<Type, bool>? predicate = null)
        => MapEndpoints(endpoints, Assembly.GetCallingAssembly(), predicate);

    public static void MapEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(assembly);

        var endpointRouteHandlerBuilderInterfaceType = typeof(IEndpointRouteHandlerBuilder);

        var endpointRouteHandlerBuilderTypes = assembly.GetTypes().Where(t =>
            t.IsClass && !t.IsAbstract && !t.IsGenericType
            && endpointRouteHandlerBuilderInterfaceType.IsAssignableFrom(t)
            && (predicate?.Invoke(t) ?? true));

        foreach (var endpointRouteHandlerBuilderType in endpointRouteHandlerBuilderTypes)
        {
            var mapEndpointsMethod = endpointRouteHandlerBuilderType.GetMethod(
                nameof(IEndpointRouteHandlerBuilder.MapEndpoints),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                binder: null,
                types: new[] { typeof(IEndpointRouteBuilder) },
                modifiers: null)!;
            mapEndpointsMethod.Invoke(null, [endpoints]);
        }
    }

    public static void MapEndpointsFromAssemblyContaining<T>(this IEndpointRouteBuilder endpoints, Func<Type, bool>? predicate = null) where T : class
        => MapEndpoints(endpoints, typeof(T).Assembly, predicate);

    public static void MapEndpoints<T>(this IEndpointRouteBuilder endpoints) where T : IEndpointRouteHandlerBuilder
        => T.MapEndpoints(endpoints);
}