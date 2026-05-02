using AppEngine.Caching.Options;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace AppEngine.Caching.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds FusionCache to the service collection with default entry options configured via the specified action.
        /// </summary>
        /// <remarks>This method registers both in-memory caching and FusionCache with default entry options based
        /// on the provided configuration. It is intended to simplify setup for common scenarios. For more advanced
        /// configuration, refer to the FusionCache documentation.</remarks>
        /// <param name="services">The service collection to which FusionCache and its dependencies will be added. Cannot be null.</param>
        /// <param name="action">An action that configures the default FusionCache entry settings. Cannot be null.</param>
        /// <returns>The same service collection instance, enabling method chaining.</returns>
        public IServiceCollection AddDefaultFusionCache(Action<FusionCacheEntrySettings> action)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            services.AddMemoryCache();
            services.AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout
                });

            return services;
        }

        /// <summary>
        /// Adds and configures FusionCache with distributed Redis support to the specified service collection.
        /// </summary>
        /// <remarks>This method registers FusionCache with both in-memory and distributed Redis caching
        /// capabilities. It allows customization of cache entry options and distributed cache settings using the provided
        /// configuration actions. Call this method during application startup to enable FusionCache with distributed
        /// support in your dependency injection container.</remarks>
        /// <param name="services">The service collection to which FusionCache and its dependencies are added. Cannot be null.</param>
        /// <param name="action">An action to configure the FusionCache entry settings. Cannot be null.</param>
        /// <param name="distributedAction">An action to configure the distributed cache settings, such as the Redis connection string. Cannot be null.</param>
        /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
        public IServiceCollection AddDistributedFusionCache(Action<FusionCacheEntrySettings> action, Action<FusionCacheDistributedSettings> distributedAction)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(distributedAction);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            var distributedSettings = new FusionCacheDistributedSettings();
            distributedAction.Invoke(distributedSettings);

            services.AddMemoryCache();
            services.AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = distributedSettings.RedisConnectionString }));

            return services;
        }

        /// <summary>
        /// Adds and configures FusionCache with distributed cache support to the service collection using the specified
        /// entry and distributed settings.
        /// </summary>
        /// <remarks>This method registers FusionCache with both in-memory and distributed cache capabilities,
        /// using System.Text.Json for serialization and Redis as the distributed cache provider. It applies the provided
        /// entry and distributed settings to configure cache behavior.</remarks>
        /// <param name="services">The service collection to which FusionCache and its distributed cache support will be added. Cannot be null.</param>
        /// <param name="action">An action to configure the FusionCache entry settings. Cannot be null.</param>
        /// <param name="distributedAction">An action to configure the distributed cache settings, such as the Redis connection string. Cannot be null.</param>
        /// <param name="options">The options instance containing distributed cache configuration values. Cannot be null.</param>
        /// <returns>The same IServiceCollection instance so that additional calls can be chained.</returns>
        public IServiceCollection AddDistributedOptionsFusionCache(Action<FusionCacheEntrySettings> action,
            Action<FusionCacheDistributedSettings> distributedAction, IOptions<FusionCacheDistributedOptions> options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);

            ArgumentNullException.ThrowIfNull(distributedAction);
            ArgumentNullException.ThrowIfNull(options);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            var distributedSettings = new FusionCacheDistributedSettings();
            distributedAction.Invoke(distributedSettings);

            var cacheOptions = options.Value;

            services.AddMemoryCache();
            services.AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout,

                    // DISTRIBUTED CACHE OPTIONS
                    DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout,
                    AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = distributedSettings.RedisConnectionString }));

            return services;
        }

        /// <summary>
        /// Adds a distributed FusionCache implementation with configurable jitter, distributed settings, and optional
        /// backplane support to the service collection.
        /// </summary>
        /// <remarks>This method configures FusionCache with support for distributed caching, optional backplane
        /// integration, and distributed stampede protection based on the provided options. It should be called during
        /// application startup to register the necessary services for distributed caching scenarios.</remarks>
        /// <param name="services">The service collection to which the FusionCache services will be added. Cannot be null.</param>
        /// <param name="action">An action to configure the FusionCache entry settings. Cannot be null.</param>
        /// <param name="distributedAction">An action to configure the distributed cache settings. Cannot be null.</param>
        /// <param name="options">The options used to configure distributed FusionCache features, including backplane and logging settings. Cannot
        /// be null.</param>
        /// <returns>The same IServiceCollection instance so that additional calls can be chained.</returns>
        public IServiceCollection AddDistributedJitterFusionCache(Action<FusionCacheEntrySettings> action,
            Action<FusionCacheDistributedSettings> distributedAction, IOptions<FusionCacheDistributedOptions> options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);

            ArgumentNullException.ThrowIfNull(distributedAction);
            ArgumentNullException.ThrowIfNull(options);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            var distributedSettings = new FusionCacheDistributedSettings();
            distributedAction.Invoke(distributedSettings);

            var cacheOptions = options.Value;
            var fusionCacheEnabledOptions = cacheOptions.FusionCacheEnabledOptions;

            var enableBackplane = fusionCacheEnabledOptions?.EnableBackplane ?? false;
            var enableDistributedStampedeProtection = fusionCacheEnabledOptions?.EnableDistributedStampedeProtection ?? false;
            var enableLogging = fusionCacheEnabledOptions?.EnableLogging ?? false;

            services.AddMemoryCache();

            if (enableBackplane)
            {
                if (enableDistributedStampedeProtection)
                {
                    if (enableLogging)
                    {
                        Task.FromResult(services.AddFusionCacheDistributedStampedeLoggingAsync(configuration, distributedSettings, cacheOptions));
                    }
                    else
                    {
                        Task.FromResult(services.AddFusionCacheDistributedStampedeProtectionAsync(configuration, distributedSettings, cacheOptions));
                    }
                }
                else
                {
                    services.AddFusionCacheWithBackplane(configuration, distributedSettings, cacheOptions);
                }
            }
            else
            {
                services.AddFusionCacheWithoutBackplane(configuration, distributedSettings, cacheOptions);
            }

            return services;
        }

        #region "Internal Methods"

        internal IServiceCollection AddFusionCacheWithoutBackplane(FusionCacheEntrySettings configuration,
            FusionCacheDistributedSettings distributedSettings, FusionCacheDistributedOptions cacheOptions)
        {
            services.AddFusionCache()
                .WithOptions(options =>
                {
                    options.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);
                })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout,

                    // DISTRIBUTED CACHE OPTIONS
                    DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout,
                    AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations,

                    // JITTERING
                    JitterMaxDuration = cacheOptions.JitterOptions?.JitterMaxDuration ?? TimeSpan.FromSeconds(2)
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = distributedSettings.RedisConnectionString }));

            return services;
        }

        internal IServiceCollection AddFusionCacheWithBackplane(FusionCacheEntrySettings configuration,
            FusionCacheDistributedSettings distributedSettings, FusionCacheDistributedOptions cacheOptions)
        {
            services.AddFusionCache()
                .WithOptions(options =>
                {
                    options.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);
                })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout,

                    // DISTRIBUTED CACHE OPTIONS
                    DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout,
                    AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations,

                    // JITTERING
                    JitterMaxDuration = cacheOptions.JitterOptions?.JitterMaxDuration ?? TimeSpan.FromSeconds(2)
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions() { Configuration = distributedSettings.RedisConnectionString }))
                // ADD BACKPLANE BASED ON REDIS
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions() { Configuration = distributedSettings.RedisConnectionString }));

            return services;
        }

        internal async Task<IServiceCollection> AddFusionCacheDistributedStampedeProtectionAsync(FusionCacheEntrySettings configuration, FusionCacheDistributedSettings distributedSettings, FusionCacheDistributedOptions cacheOptions)
        {
            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            IConnectionMultiplexer muxer = await ConnectionMultiplexer.ConnectAsync(distributedSettings.RedisConnectionString);

            services.AddFusionCache()
                .WithOptions(options =>
                {
                    options.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);
                })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout,

                    // DISTRIBUTED CACHE OPTIONS
                    DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout,
                    AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations,

                    // JITTERING
                    JitterMaxDuration = cacheOptions.JitterOptions?.JitterMaxDuration ?? TimeSpan.FromSeconds(2)
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }))
                // ADD BACKPLANE BASED ON REDIS
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }))
                // ADD THE DISTRIBUTED LOCKER BASED ON REDIS
                .WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }));

            return services;
        }

        internal async Task<IServiceCollection> AddFusionCacheDistributedStampedeLoggingAsync(FusionCacheEntrySettings configuration, FusionCacheDistributedSettings distributedSettings, FusionCacheDistributedOptions cacheOptions)
        {
            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            IConnectionMultiplexer muxer = await ConnectionMultiplexer.ConnectAsync(distributedSettings.RedisConnectionString);

            var logOptions = cacheOptions.FusionCacheEnabledOptions?.LoggingOptions;

            services.AddFusionCache()
                .WithOptions(options =>
                {
                    options.DistributedCacheCircuitBreakerDuration = cacheOptions.JitterOptions?.DistributedCacheCircuitBreakerDuration ?? TimeSpan.FromSeconds(2);

                    // CUSTOM LOG LEVELS
                    options.FailSafeActivationLogLevel = logOptions?.FailSafeActivationLogLevel ?? LogLevel.Debug;
                    options.SerializationErrorsLogLevel = logOptions?.SerializationErrorsLogLevel ?? LogLevel.Warning;

                    options.DistributedCacheSyntheticTimeoutsLogLevel = logOptions?.DistributedCacheSyntheticTimeoutsLogLevel ?? LogLevel.Debug;
                    options.DistributedCacheErrorsLogLevel = logOptions?.DistributedCacheErrorsLogLevel ?? LogLevel.Error;

                    options.FactorySyntheticTimeoutsLogLevel = logOptions?.FactorySyntheticTimeoutsLogLevel ?? LogLevel.Debug;
                    options.FactoryErrorsLogLevel = logOptions?.FactoryErrorsLogLevel ?? LogLevel.Error;
                })
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = configuration.Duration,

                    // FAIL-SAFE OPTIONS
                    IsFailSafeEnabled = configuration.IsFailSafeEnabled,
                    FailSafeMaxDuration = configuration.FailSafeMaxDuration,
                    FailSafeThrottleDuration = configuration.FailSafeThrottleDuration,

                    // EAGER REFRESH
                    EagerRefreshThreshold = configuration.EagerRefreshThreshold,

                    // FACTORY TIMEOUTS
                    FactorySoftTimeout = configuration.FactorySoftTimeout,
                    FactoryHardTimeout = configuration.FactoryHardTimeout,

                    // DISTRIBUTED CACHE OPTIONS
                    DistributedCacheSoftTimeout = cacheOptions.DistributedCacheSoftTimeout,
                    DistributedCacheHardTimeout = cacheOptions.DistributedCacheHardTimeout,
                    AllowBackgroundDistributedCacheOperations = cacheOptions.AllowBackgroundDistributedCacheOperations,

                    // JITTERING
                    JitterMaxDuration = cacheOptions.JitterOptions?.JitterMaxDuration ?? TimeSpan.FromSeconds(2)
                })
                // ADD SERIALIZATION BASED ON System.Text.Json
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                // ADD DISTRIBUTED CACHE BASED ON REDIS
                .WithDistributedCache(new RedisCache(new RedisCacheOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }))
                // ADD BACKPLANE BASED ON REDIS
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }))
                // ADD THE DISTRIBUTED LOCKER BASED ON REDIS
                .WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }));

            return services;
        }

        #endregion
    }
}