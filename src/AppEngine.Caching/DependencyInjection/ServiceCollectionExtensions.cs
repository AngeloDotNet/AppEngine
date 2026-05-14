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
        //var cacheOptions = new CacheOptions
        //{
        //    EnableOptions = optionsSection.GetValue<bool>("EnableOptions"),
        //    EnableDefaultEntryOptions = optionsSection.GetValue<bool>("EnableDefaultEntryOptions"),
        //    EnableDistributedCache = optionsSection.GetValue<bool>("EnableDistributedCache"),
        //    EnableBackplane = optionsSection.GetValue<bool>("EnableBackplane"),
        //    EnableDistributedLocker = optionsSection.GetValue<bool>("EnableDistributedLocker"),
        //    ListEntryOptions = optionsSection.GetValue<List<FusionCacheEntryEnumOptions>>("ListEntryOptions") ?? [],
        //    EnableSystemTextJsonSerializer = optionsSection.GetValue<bool>("EnableSystemTextJsonSerializer"),
        //};

        var fusionCacheEntrySettings = configuration.GetSection("FusionCacheEntrySettings");
        var entryOptions = ServiceMaterializedExtensions.GetFusionCacheEntrySettings(fusionCacheEntrySettings);
        //var entryOptions = new FusionCacheEntrySettings()
        //{
        //    Duration = TryParseTimeSpan(fusionCacheEntrySettings["Duration"], TimeSpan.FromMinutes(5)),
        //    IsFailSafeEnabled = fusionCacheEntrySettings.GetSection("IsFailSafeEnabled").Get<bool>(),
        //    FailSafeMaxDuration = TryParseTimeSpan(fusionCacheEntrySettings["FailSafeMaxDuration"], TimeSpan.FromHours(1)),
        //    FailSafeThrottleDuration = TryParseTimeSpan(fusionCacheEntrySettings["FailSafeThrottleDuration"], TimeSpan.FromMinutes(5)),
        //    EagerRefreshThreshold = TryParseFloat(fusionCacheEntrySettings["EagerRefreshThreshold"], 0.9f),
        //    FactorySoftTimeout = TryParseTimeSpan(fusionCacheEntrySettings["FactorySoftTimeout"], TimeSpan.FromSeconds(1)),
        //    FactoryHardTimeout = TryParseTimeSpan(fusionCacheEntrySettings["FactoryHardTimeout"], TimeSpan.FromSeconds(5))
        //};

        cacheOptions.Configuration = entryOptions;

        var fusionCacheLoggingOptions = configuration.GetSection("FusionCacheLoggingOptions");
        var loggingOptions = ServiceMaterializedExtensions.GetFusionCacheLoggingOptions(fusionCacheLoggingOptions);
        //var loggingOptions = new FusionCacheLoggingOptions()
        //{
        //    FailSafeActivationLogLevel = TryParseEnum(fusionCacheLoggingOptions["FailSafeActivationLogLevel"], LogLevel.Debug),
        //    SerializationErrorsLogLevel = TryParseEnum(fusionCacheLoggingOptions["SerializationErrorsLogLevel"], LogLevel.Warning),
        //    DistributedCacheSyntheticTimeoutsLogLevel = TryParseEnum(fusionCacheLoggingOptions["DistributedCacheSyntheticTimeoutsLogLevel"], LogLevel.Debug),
        //    DistributedCacheErrorsLogLevel = TryParseEnum(fusionCacheLoggingOptions["DistributedCacheErrorsLogLevel"], LogLevel.Error),
        //    FactorySyntheticTimeoutsLogLevel = TryParseEnum(fusionCacheLoggingOptions["FactorySyntheticTimeoutsLogLevel"], LogLevel.Debug),
        //    FactoryErrorsLogLevel = TryParseEnum(fusionCacheLoggingOptions["FactoryErrorsLogLevel"], LogLevel.Error)
        //};

        var fusionCacheEnabledOptions = configuration.GetSection("FusionCacheEnabledOptions");
        var enabledOptions = ServiceMaterializedExtensions.GetFusionCacheEnabledOptions(fusionCacheEnabledOptions);
        //var enabledOptions = new FusionCacheEnabledOptions()
        //{
        //    EnableAllFusionCacheOptions = fusionCacheEnabledOptions.GetValue<bool>("EnableAllFusionCacheOptions"),
        //    EnableDefaultEntryOptions = fusionCacheEnabledOptions.GetValue<bool>("EnableDefaultEntryOptions"),
        //    EnableSystemTextJsonSerializer = fusionCacheEnabledOptions.GetValue<bool>("EnableSystemTextJsonSerializer"),
        //    EnabledEntryOptions = fusionCacheEnabledOptions.GetValue<List<FusionCacheEntryEnumOptions>>("EnabledEntryOptions") ?? [],
        //    DistributedCacheType = TryParseEnum(fusionCacheEnabledOptions["DistributedCacheType"], DistributedCacheEnum.None),
        //    EnableDistributedCache = fusionCacheEnabledOptions.GetValue<bool>("EnableDistributedCache"),
        //    EnableBackplane = fusionCacheEnabledOptions.GetValue<bool>("EnableBackplane"),
        //    EnableDistributedLocker = fusionCacheEnabledOptions.GetValue<bool>("EnableDistributedLocker"),
        //    LoggingOptions = loggingOptions
        //};

        var fusionCacheJitterOptions = configuration.GetSection("JitterOptions");
        var jitterOptions = ServiceMaterializedExtensions.GetFusionCacheJitterOptions(fusionCacheJitterOptions);
        //var jitterOptions = new FusionCacheJitterOptions()
        //{
        //    DistributedCacheCircuitBreakerDuration = TryParseTimeSpan(fusionCacheJitterOptions["DistributedCacheCircuitBreakerDuration"], TimeSpan.FromSeconds(2)),
        //    JitterMaxDuration = TryParseTimeSpan(fusionCacheJitterOptions["JitterMaxDuration"], TimeSpan.FromSeconds(2))
        //};

        var fusionCacheDistributedOptions = configuration.GetSection("FusionCacheDistributedOptions");
        var distributedOptions = ServiceMaterializedExtensions.GetFusionCacheDistributedOptions(fusionCacheDistributedOptions);
        //var distributedOptions = new FusionCacheDistributedOptions()
        //{
        //    DistributedCacheSoftTimeout = TryParseTimeSpan(fusionCacheDistributedOptions["DistributedCacheSoftTimeout"], TimeSpan.FromSeconds(1)),
        //    DistributedCacheHardTimeout = TryParseTimeSpan(fusionCacheDistributedOptions["DistributedCacheHardTimeout"], TimeSpan.FromSeconds(5)),
        //    AllowBackgroundDistributedCacheOperations = fusionCacheDistributedOptions.GetValue<bool>("AllowBackgroundDistributedCacheOperations"),
        //    JitterOptions = jitterOptions,
        //    FusionCacheEnabledOptions = enabledOptions,
        //    CachePath = fusionCacheDistributedOptions["CachePath"] ?? string.Empty,
        //};

        //cacheOptions.CacheDistributedOptions = distributedOptions;

        //cacheOptions.RedisCacheOptions = new RedisCacheOptions()
        //{
        //    Configuration = configuration["RedisCacheOptions:Configuration"],
        //};

        //cacheOptions.RedisBackplaneOptions = new RedisBackplaneOptions()
        //{
        //    Configuration = configuration["RedisBackplaneOptions:Configuration"],
        //};

        //cacheOptions.MuxerRedisConnectionString = configuration["MuxerRedisConnectionString"] ?? string.Empty;
        //cacheOptions.TypeDistributedCache = TryParseEnum(configuration["TypeDistributedCache"], DistributedCacheEnum.None);

        cacheOptions.CacheDistributedOptions = distributedOptions;

        var otherCacheOptions = ServiceMaterializedExtensions.LoadCacheOptions(configuration, cacheOptions);

        cacheOptions.RedisCacheOptions = otherCacheOptions.RedisCacheOptions;
        cacheOptions.RedisBackplaneOptions = otherCacheOptions.RedisBackplaneOptions;

        cacheOptions.MuxerRedisConnectionString = configuration["MuxerRedisConnectionString"] ?? string.Empty;
        cacheOptions.TypeDistributedCache = otherCacheOptions.TypeDistributedCache;

        return cacheOptions;

        //static TimeSpan TryParseTimeSpan(string? value, TimeSpan defaultValue)
        //{
        //    return TimeSpan.TryParse(value, out var result) ? result : defaultValue;
        //}

        //static float TryParseFloat(string? value, float defaultValue)
        //{
        //    return float.TryParse(value, out var result) ? result : defaultValue;
        //}

        //static TEnum TryParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
        //{
        //    return Enum.TryParse<TEnum>(value, out var result) ? result : defaultValue;
        //}
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