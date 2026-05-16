using AppEngine.Caching.Enums;
using AppEngine.Caching.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.NewtonsoftJson;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace AppEngine.Caching.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // TODO:
    // - Cache Levels (Distributed, Local, Hybrid)
    // - Cache Stampede protection with Redis (with and without backplane)
    // - Conditional refresh
    // - Tagging support

    public static CacheOptions GetCustomFusionCache(IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection("CacheOptions");
        var cacheOptions = ServiceMaterializedExtensions.GetCacheOptions(optionsSection);

        var fusionCacheEntrySettings = configuration.GetSection("FusionCacheEntrySettings");
        var entryOptions = ServiceMaterializedExtensions.GetFusionCacheEntrySettings(fusionCacheEntrySettings);

        cacheOptions.Configuration = entryOptions;

        var fusionCacheLoggingOptions = configuration.GetSection("FusionCacheLoggingOptions");
        var loggingOptions = ServiceMaterializedExtensions.GetFusionCacheLoggingOptions(fusionCacheLoggingOptions);

        var fusionCacheEnabledOptions = configuration.GetSection("FusionCacheEnabledOptions");
        var enabledOptions = ServiceMaterializedExtensions.GetFusionCacheEnabledOptions(fusionCacheEnabledOptions);

        var fusionCacheJitterOptions = configuration.GetSection("JitterOptions");
        var jitterOptions = ServiceMaterializedExtensions.GetFusionCacheJitterOptions(fusionCacheJitterOptions);

        var fusionCacheDistributedOptions = configuration.GetSection("FusionCacheDistributedOptions");
        var distributedOptions = ServiceMaterializedExtensions.GetFusionCacheDistributedOptions(fusionCacheDistributedOptions);

        cacheOptions.CacheDistributedOptions = distributedOptions;

        var otherCacheOptions = ServiceMaterializedExtensions.LoadCacheOptions(configuration, cacheOptions);

        cacheOptions.RedisCacheOptions = otherCacheOptions.RedisCacheOptions;
        cacheOptions.RedisBackplaneOptions = otherCacheOptions.RedisBackplaneOptions;

        cacheOptions.MuxerRedisConnectionString = configuration["MuxerRedisConnectionString"] ?? string.Empty;
        cacheOptions.TypeDistributedCache = otherCacheOptions.TypeDistributedCache;

        return cacheOptions;
    }

    extension(IServiceCollection services)
    {
        public async Task<IServiceCollection> AddFusionCacheWithOptionsAsync(CacheOptions options)
        {
            var builder = services
                .AddMemoryCache()
                .AddFusionCache();

            if (options.EnableOptions)
            {
                builder.AddOptions(options.CacheDistributedOptions, FusionCacheEnumOptions.EnableAllFusionCacheOptions);
            }

            if (options.EnableDefaultEntryOptions)
            {
                builder.AddDefaultEntryOptions(options.CacheDistributedOptions, options.Configuration, options.ListEntryOptions);
            }

            if (options.EnableSystemTextJsonSerializer)
            {
                builder.WithSerializer(new FusionCacheSystemTextJsonSerializer());
            }
            else
            {
                builder.WithSerializer(new FusionCacheNewtonsoftJsonSerializer());
            }

            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            var muxer = options.RedisCacheOptions.ConnectionMultiplexerFactory?.Invoke().Result;

            if (muxer is not null)
            {
                muxer = await ConnectionMultiplexer.ConnectAsync(options.MuxerRedisConnectionString);
            }

            var redisCacheOptions = options.RedisCacheOptions.Configuration;

            if (redisCacheOptions is null)
            {
                options.RedisCacheOptions.Configuration = string.Empty;
            }

            if (options.EnableDistributedCache)
            {
                builder.AddDistributedCache(options.CacheDistributedOptions, options.RedisCacheOptions, options.TypeDistributedCache, muxer);
            }

            var redisBackplaneOptions = options.RedisBackplaneOptions.Configuration;

            if (redisBackplaneOptions is null)
            {
                options.RedisBackplaneOptions.Configuration = string.Empty;
            }

            if (options.EnableBackplane)
            {
                builder.AddRedisBackplane(options.RedisBackplaneOptions, muxer);
            }

            if (options.EnableDistributedLocker)
            {
                builder.AddDistributedLocker(options.RedisBackplaneOptions, muxer);
            }

            return builder.Services;
        }
    }
}