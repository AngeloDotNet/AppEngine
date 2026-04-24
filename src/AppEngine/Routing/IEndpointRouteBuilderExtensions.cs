using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace AppEngine.Routing;

public static class IEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public void MapEndpoints(Func<Type, bool>? predicate = null) => MapEndpoints(endpoints, Assembly.GetCallingAssembly(), predicate);

        public void MapEndpoints(Assembly assembly, Func<Type, bool>? predicate = null)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentNullException.ThrowIfNull(assembly);

            var endpointRouteHandlerBuilderInterfaceType = typeof(IEndpointRouteHandlerBuilder);

            var endpointRouteHandlerBuilderTypes = assembly.GetTypes().Where(t
                => t.IsClass && !t.IsAbstract && !t.IsGenericType && endpointRouteHandlerBuilderInterfaceType.IsAssignableFrom(t)
                    && (predicate?.Invoke(t) ?? true));

            foreach (var endpointRouteHandlerBuilderType in endpointRouteHandlerBuilderTypes)
            {
                var mapEndpointsMethod = endpointRouteHandlerBuilderType.GetMethod(nameof(IEndpointRouteHandlerBuilder.MapEndpoints),
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;

                mapEndpointsMethod.Invoke(null, [endpoints]);
            }
        }

        public void MapEndpointsFromAssemblyContaining<T>(Func<Type, bool>? predicate = null) where T : class
            => MapEndpoints(endpoints, typeof(T).Assembly, predicate);

        public void MapEndpoints<T>() where T : IEndpointRouteHandlerBuilder => T.MapEndpoints(endpoints);
    }
}