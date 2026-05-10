using AppEngine.Caching.Enums;
using AppEngine.Caching.Options;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace AppEngine.Caching.DependencyInjection;

public static class ServiceMaterializedOptions
{
    public static IFusionCacheBuilder AddOptions(this IFusionCacheBuilder builder, FusionCacheDistributedOptions cacheOptions,
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

    public static IFusionCacheBuilder AddDefaultEntryOptions(this IFusionCacheBuilder builder, FusionCacheDistributedOptions cacheOptions,
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

    private static void ConfigureLogLevels(FusionCacheOptions options, FusionCacheLoggingOptions? logOptions)
    {
        options.FailSafeActivationLogLevel = logOptions?.FailSafeActivationLogLevel ?? LogLevel.Debug;
        options.SerializationErrorsLogLevel = logOptions?.SerializationErrorsLogLevel ?? LogLevel.Warning;

        options.DistributedCacheSyntheticTimeoutsLogLevel = logOptions?.DistributedCacheSyntheticTimeoutsLogLevel ?? LogLevel.Debug;
        options.DistributedCacheErrorsLogLevel = logOptions?.DistributedCacheErrorsLogLevel ?? LogLevel.Error;

        options.FactorySyntheticTimeoutsLogLevel = logOptions?.FactorySyntheticTimeoutsLogLevel ?? LogLevel.Debug;
        options.FactoryErrorsLogLevel = logOptions?.FactoryErrorsLogLevel ?? LogLevel.Error;
    }
}