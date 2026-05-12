using AppEngine.Caching.Enums;
using AppEngine.Caching.Options;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using NeoSmart.Caching.Sqlite;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;

namespace AppEngine.Caching.DependencyInjection;

internal static class ServiceMaterializedOptions
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
                builder.WithDistributedCache(new RedisCache(redisOptions));
            }
        }

        if (typeDistributedCache == DistributedCacheEnum.SQLite)
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
            builder.WithBackplane(new RedisBackplane(redisBackplaneOptions));
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
            builder.WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
            {
                ConfigurationOptions = new ConfigurationOptions
                {
                    EndPoints = { redisBackplaneOptions.Configuration! },
                    Password = redisBackplaneOptions.ConfigurationOptions!.Password,
                    Ssl = redisBackplaneOptions.ConfigurationOptions!.Ssl,
                    ConnectTimeout = redisBackplaneOptions.ConfigurationOptions!.ConnectTimeout,
                    SyncTimeout = redisBackplaneOptions.ConfigurationOptions!.SyncTimeout,
                    AbortOnConnectFail = redisBackplaneOptions.ConfigurationOptions!.AbortOnConnectFail
                }
            }));
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
}