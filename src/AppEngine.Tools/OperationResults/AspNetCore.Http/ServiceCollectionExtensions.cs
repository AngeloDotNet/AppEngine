using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppEngine.Tools.OperationResults.AspNetCore.Http;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOperationResult(Action<OperationResultOptions>? configuration = null)
        {
            var operationResultOptions = new OperationResultOptions();
            configuration?.Invoke(operationResultOptions);

            services.TryAddSingleton(operationResultOptions);

            return services;
        }
    }
}