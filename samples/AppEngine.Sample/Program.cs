using AppEngine.Caching.DependencyInjection;

namespace AppEngine.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();

        var configuration = builder.Configuration;

        var fusionCacheConfig = ServiceCollectionExtensions.GetCustomFusionCache(configuration);
        await builder.Services.AddFusionCacheWithOptionsAsync(fusionCacheConfig);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(config => config.SwaggerEndpoint("/openapi/v1.json", $"{app.Environment.ApplicationName} v1"));
        }

        app.Run();
    }
}