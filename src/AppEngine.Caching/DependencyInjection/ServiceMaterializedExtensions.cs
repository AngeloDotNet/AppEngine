using AppEngine.Caching.Enums;
using AppEngine.Caching.Options;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoSmart.Caching.Sqlite;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;

namespace AppEngine.Caching.DependencyInjection;

internal static class ServiceMaterializedExtensions
{
    internal static IFusionCacheBuilder AddOptions(this IFusionCacheBuilder builder, FusionCacheDistributedOptions cacheOptions,
        FusionCacheEnumOptions fusionCacheOptions)
    {
        var customOptions = new FusionCacheOptions();

        switch (fusionCacheOptions)
        {
            case FusionCacheEnumOptions.EnableDistributedCacheCircuitBreakerDuration:
                customOptions.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);
                break;

            case FusionCacheEnumOptions.EnableCustomLogLevels:
                ConfigureLogLevels(customOptions, cacheOptions.FusionCacheEnabledOptions?.LoggingOptions);
                break;

            case FusionCacheEnumOptions.EnableAllFusionCacheOptions:
                customOptions.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);
                ConfigureLogLevels(customOptions, cacheOptions.FusionCacheEnabledOptions?.LoggingOptions);
                break;
        }

        builder.Options = customOptions;

        return builder;
    }

    internal static IFusionCacheBuilder AddDefaultEntryOptions(this IFusionCacheBuilder builder, FusionCacheDistributedOptions cacheOptions,
        FusionCacheEntrySettings configuration, List<FusionCacheEntryEnumOptions> entryOptions)
    {
        var options = new FusionCacheEntryOptions();

        foreach (var entry in entryOptions)
        {
            switch (entry)
            {
                case FusionCacheEntryEnumOptions.EnableDuration:
                    options.Duration = configuration.Duration;
                    break;

                case FusionCacheEntryEnumOptions.EnableFailSafe:
                    options.IsFailSafeEnabled = configuration.IsFailSafeEnabled;
                    options.FailSafeMaxDuration = configuration.FailSafeMaxDuration;
                    options.FailSafeThrottleDuration = configuration.FailSafeThrottleDuration;
                    break;

                case FusionCacheEntryEnumOptions.EnableEagerRefresh:
                    options.EagerRefreshThreshold = configuration.EagerRefreshThreshold;
                    break;

                case FusionCacheEntryEnumOptions.EnableFactoryTimeout:
                    options.FactorySoftTimeout = configuration.FactorySoftTimeout;
                    options.FactoryHardTimeout = configuration.FactoryHardTimeout;
                    break;

                case FusionCacheEntryEnumOptions.EnableDistributedCache:
                    options.DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout;
                    options.DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout;
                    options.AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations;
                    break;

                case FusionCacheEntryEnumOptions.EnableJitter:
                    options.JitterMaxDuration = cacheOptions.JitterOptions?.JitterMaxDuration ?? TimeSpan.FromSeconds(2);
                    break;
            }
        }

        builder.DefaultEntryOptions = options;
        return builder;
    }

    internal static IFusionCacheBuilder AddDistributedCache(this IFusionCacheBuilder builder, FusionCacheDistributedOptions cacheOptions,
        RedisCacheOptions redisOptions, DistributedCacheEnum typeDistributedCache, IConnectionMultiplexer muxer)
    {
        if (typeDistributedCache == DistributedCacheEnum.Redis)
        {
            if (muxer is not null)
            {
                builder.WithDistributedCache(new RedisCache(new RedisCacheOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }));
            }
            else
            {
                builder.WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = redisOptions.Configuration }));
            }
        }

        if (typeDistributedCache == DistributedCacheEnum.SQLite && !string.IsNullOrEmpty(cacheOptions.CachePath))
        {
            builder.WithDistributedCache(new SqliteCache(new SqliteCacheOptions { CachePath = cacheOptions.CachePath }));
        }

        return builder;
    }

    internal static IFusionCacheBuilder AddRedisBackplane(this IFusionCacheBuilder builder, RedisBackplaneOptions redisBackplaneOptions,
        IConnectionMultiplexer muxer)
    {
        if (muxer is not null)
        {
            builder.WithBackplane(new RedisBackplane(new RedisBackplaneOptions()
            {
                ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
            }));
        }

        if (muxer is null)
        {
            builder.WithBackplane(new RedisBackplane(new RedisBackplaneOptions() { Configuration = redisBackplaneOptions.Configuration }));
        }

        return builder;
    }

    internal static IFusionCacheBuilder AddDistributedLocker(this IFusionCacheBuilder builder, RedisBackplaneOptions redisBackplaneOptions,
        IConnectionMultiplexer muxer)
    {
        if (muxer is not null)
        {
            builder.WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
            {
                ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
            }));
        }

        if (muxer is null)
        {
            //builder.WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
            //{
            //    ConfigurationOptions = new ConfigurationOptions
            //    {
            //        EndPoints = { redisBackplaneOptions.Configuration! },
            //        Password = redisBackplaneOptions.ConfigurationOptions!.Password,
            //        Ssl = redisBackplaneOptions.ConfigurationOptions!.Ssl,
            //        ConnectTimeout = redisBackplaneOptions.ConfigurationOptions!.ConnectTimeout,
            //        SyncTimeout = redisBackplaneOptions.ConfigurationOptions!.SyncTimeout,
            //        AbortOnConnectFail = redisBackplaneOptions.ConfigurationOptions!.AbortOnConnectFail
            //    }
            //}));
            builder.WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions { Configuration = redisBackplaneOptions.Configuration }));
        }

        return builder;
    }

    internal static void ConfigureLogLevels(FusionCacheOptions options, FusionCacheLoggingOptions? logOptions)
    {
        options.FailSafeActivationLogLevel = logOptions?.FailSafeActivationLogLevel ?? LogLevel.Debug;
        options.SerializationErrorsLogLevel = logOptions?.SerializationErrorsLogLevel ?? LogLevel.Warning;

        options.DistributedCacheSyntheticTimeoutsLogLevel = logOptions?.DistributedCacheSyntheticTimeoutsLogLevel ?? LogLevel.Debug;
        options.DistributedCacheErrorsLogLevel = logOptions?.DistributedCacheErrorsLogLevel ?? LogLevel.Error;

        options.FactorySyntheticTimeoutsLogLevel = logOptions?.FactorySyntheticTimeoutsLogLevel ?? LogLevel.Debug;
        options.FactoryErrorsLogLevel = logOptions?.FactoryErrorsLogLevel ?? LogLevel.Error;
    }

    #region "Service Collection Extensions Methods"

    internal static CacheOptions GetCacheOptions(IConfiguration configuration)
    {
        return new CacheOptions
        {
            EnableOptions = configuration.GetValue<bool>("EnableOptions"),
            EnableDefaultEntryOptions = configuration.GetValue<bool>("EnableDefaultEntryOptions"),
            EnableDistributedCache = configuration.GetValue<bool>("EnableDistributedCache"),
            EnableBackplane = configuration.GetValue<bool>("EnableBackplane"),
            EnableDistributedLocker = configuration.GetValue<bool>("EnableDistributedLocker"),
            ListEntryOptions = configuration.GetValue<List<FusionCacheEntryEnumOptions>>("ListEntryOptions") ?? [],
            EnableSystemTextJsonSerializer = configuration.GetValue<bool>("EnableSystemTextJsonSerializer"),
        };
    }

    internal static FusionCacheEntrySettings GetFusionCacheEntrySettings(IConfiguration configuration)
    {
        return new FusionCacheEntrySettings()
        {
            Duration = TryParseTimeSpan(configuration["Duration"], TimeSpan.FromMinutes(5)),
            IsFailSafeEnabled = configuration.GetSection("IsFailSafeEnabled").Get<bool>(),
            FailSafeMaxDuration = TryParseTimeSpan(configuration["FailSafeMaxDuration"], TimeSpan.FromHours(1)),
            FailSafeThrottleDuration = TryParseTimeSpan(configuration["FailSafeThrottleDuration"], TimeSpan.FromMinutes(5)),
            EagerRefreshThreshold = TryParseFloat(configuration["EagerRefreshThreshold"], 0.9f),
            FactorySoftTimeout = TryParseTimeSpan(configuration["FactorySoftTimeout"], TimeSpan.FromSeconds(1)),
            FactoryHardTimeout = TryParseTimeSpan(configuration["FactoryHardTimeout"], TimeSpan.FromSeconds(5))
        };
    }

    internal static FusionCacheLoggingOptions GetFusionCacheLoggingOptions(IConfiguration configuration)
    {
        return new FusionCacheLoggingOptions()
        {
            FailSafeActivationLogLevel = TryParseEnum(configuration["FailSafeActivationLogLevel"], LogLevel.Debug),
            SerializationErrorsLogLevel = TryParseEnum(configuration["SerializationErrorsLogLevel"], LogLevel.Warning),
            DistributedCacheSyntheticTimeoutsLogLevel = TryParseEnum(configuration["DistributedCacheSyntheticTimeoutsLogLevel"], LogLevel.Debug),
            DistributedCacheErrorsLogLevel = TryParseEnum(configuration["DistributedCacheErrorsLogLevel"], LogLevel.Error),
            FactorySyntheticTimeoutsLogLevel = TryParseEnum(configuration["FactorySyntheticTimeoutsLogLevel"], LogLevel.Debug),
            FactoryErrorsLogLevel = TryParseEnum(configuration["FactoryErrorsLogLevel"], LogLevel.Error)
        };
    }

    internal static FusionCacheEnabledOptions GetFusionCacheEnabledOptions(IConfiguration configuration)
    {
        return new FusionCacheEnabledOptions()
        {
            EnableAllFusionCacheOptions = configuration.GetValue<bool>("EnableAllFusionCacheOptions"),
            EnableDefaultEntryOptions = configuration.GetValue<bool>("EnableDefaultEntryOptions"),
            EnableSystemTextJsonSerializer = configuration.GetValue<bool>("EnableSystemTextJsonSerializer"),
            EnabledEntryOptions = configuration.GetValue<List<FusionCacheEntryEnumOptions>>("EnabledEntryOptions") ?? [],
            DistributedCacheType = TryParseEnum(configuration["DistributedCacheType"], DistributedCacheEnum.None),
            EnableDistributedCache = configuration.GetValue<bool>("EnableDistributedCache"),
            EnableBackplane = configuration.GetValue<bool>("EnableBackplane"),
            EnableDistributedLocker = configuration.GetValue<bool>("EnableDistributedLocker"),
            LoggingOptions = GetFusionCacheLoggingOptions(configuration)
        };
    }

    internal static FusionCacheJitterOptions GetFusionCacheJitterOptions(IConfiguration configuration)
    {
        return new FusionCacheJitterOptions()
        {
            DistributedCacheCircuitBreakerDuration = TryParseTimeSpan(configuration["DistributedCacheCircuitBreakerDuration"], TimeSpan.FromSeconds(2)),
            JitterMaxDuration = TryParseTimeSpan(configuration["JitterMaxDuration"], TimeSpan.FromSeconds(2))
        };
    }

    internal static FusionCacheDistributedOptions GetFusionCacheDistributedOptions(IConfiguration configuration)
    {
        return new FusionCacheDistributedOptions()
        {
            DistributedCacheSoftTimeout = TryParseTimeSpan(configuration["DistributedCacheSoftTimeout"], TimeSpan.FromSeconds(1)),
            DistributedCacheHardTimeout = TryParseTimeSpan(configuration["DistributedCacheHardTimeout"], TimeSpan.FromSeconds(5)),
            AllowBackgroundDistributedCacheOperations = configuration.GetValue<bool>("AllowBackgroundDistributedCacheOperations"),
            JitterOptions = GetFusionCacheJitterOptions(configuration),
            FusionCacheEnabledOptions = GetFusionCacheEnabledOptions(configuration),
            CachePath = configuration["CachePath"] ?? string.Empty,
        };
    }

    internal static CacheOptions LoadCacheOptions(IConfiguration configuration, CacheOptions cacheOptions)
    {
        cacheOptions.RedisCacheOptions = new RedisCacheOptions()
        {
            Configuration = configuration["RedisCacheOptions:Configuration"],
        };

        cacheOptions.RedisBackplaneOptions = new RedisBackplaneOptions()
        {
            Configuration = configuration["RedisBackplaneOptions:Configuration"],
        };

        cacheOptions.TypeDistributedCache = TryParseEnum(configuration["TypeDistributedCache"], DistributedCacheEnum.None);

        return cacheOptions;
    }

    #endregion

    #region "Try Parse Enums"

    internal static TimeSpan TryParseTimeSpan(string? value, TimeSpan defaultValue)
    {
        return TimeSpan.TryParse(value, out var result) ? result : defaultValue;
    }

    internal static float TryParseFloat(string? value, float defaultValue)
    {
        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    internal static TEnum TryParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, out var result) ? result : defaultValue;
    }

    #endregion
}