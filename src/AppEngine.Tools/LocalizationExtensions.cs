using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace AppEngine.Tools;

public static class LocalizationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRequestLocalization(params string[] cultures) => services.AddRequestLocalization(cultures, null);

        public IServiceCollection AddRequestLocalization(IEnumerable<string> cultures, Action<IList<IRequestCultureProvider>>? providersConfiguration)
        {
            var supportedCultures = cultures.Select((string c) => new CultureInfo(c)).ToList();

            services.Configure(delegate (RequestLocalizationOptions options)
            {
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.DefaultRequestCulture = new RequestCulture(supportedCultures.First());
                providersConfiguration?.Invoke(options.RequestCultureProviders);
            });

            return services;
        }
    }
}