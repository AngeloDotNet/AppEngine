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
    /// <summary>
    /// Creates a new instance of RedisCacheOptions configured with the specified instance name and connection string.
    /// </summary>
    /// <param name="instanceName">The name to associate with the Redis cache instance. This value is used to distinguish cache data for different
    /// application instances.</param>
    /// <param name="redisConnectionString">The connection string used to connect to the Redis server. Must be a valid Redis connection string.</param>
    /// <returns>A RedisCacheOptions object initialized with the provided instance name and connection string.</returns>
    public static RedisCacheOptions GetRedisConnectionOptions(string instanceName, string redisConnectionString)
    {
        return new RedisCacheOptions
        {
            InstanceName = instanceName,
            Configuration = redisConnectionString
        };
    }

    /// <summary>
    /// Creates a new instance of RedisBackplaneOptions configured with the specified Redis connection string.
    /// </summary>
    /// <param name="redisBackplaneConnectionString">The connection string used to configure the Redis backplane. Cannot be null or empty.</param>
    /// <returns>A RedisBackplaneOptions instance initialized with the provided connection string.</returns>
    public static RedisBackplaneOptions GetRedisBackplaneConnectionOptions(string redisBackplaneConnectionString)
    {
        return new RedisBackplaneOptions
        {
            Configuration = redisBackplaneConnectionString
        };
    }

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
        /// Adds and configures a distributed FusionCache instance with custom entry and distributed cache settings to
        /// the service collection.
        /// </summary>
        /// <remarks>This method registers both in-memory and distributed (Redis-based) FusionCache
        /// services using the provided configuration delegates. It enables advanced caching scenarios by allowing
        /// customization of both local and distributed cache behaviors.</remarks>
        /// <param name="action">A delegate that configures the FusionCache entry settings. Used to specify cache entry options such as
        /// duration, fail-safe behavior, and refresh thresholds.</param>
        /// <param name="distributedAction">A delegate that configures the distributed cache settings for FusionCache, including Redis connection
        /// details and instance name.</param>
        /// <returns>The same IServiceCollection instance, enabling further configuration chaining.</returns>
        public IServiceCollection AddDistributedFusionCache(Action<FusionCacheEntrySettings> action, Action<FusionCacheDistributedSettings> distributedAction)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(distributedAction);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            var distributedSettings = new FusionCacheDistributedSettings();
            distributedAction.Invoke(distributedSettings);

            var redisOptions = GetRedisConnectionOptions(distributedSettings.InstanceName, distributedSettings.RedisConnectionString);

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
                .WithDistributedCache(new RedisCache(redisOptions));

            return services;
        }

        /// <summary>
        /// Adds and configures FusionCache with distributed cache support using the specified entry and distributed
        /// settings.
        /// </summary>
        /// <remarks>This method registers FusionCache with both in-memory and Redis-based distributed
        /// caching, applying the provided configuration actions. It also sets up serialization using System.Text.Json.
        /// Call this method during application startup to enable distributed caching with custom settings.</remarks>
        /// <param name="action">An action to configure the FusionCache entry settings, such as cache duration and fail-safe options.</param>
        /// <param name="distributedAction">An action to configure distributed cache settings, including instance name and Redis connection details.</param>
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
            var redisOptions = GetRedisConnectionOptions(distributedSettings.InstanceName, distributedSettings.RedisConnectionString);

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
                .WithDistributedCache(new RedisCache(redisOptions));

            return services;
        }

        /// <summary>
        /// Adds and configures a distributed FusionCache instance with jitter and backplane support to the service
        /// collection.
        /// </summary>
        /// <remarks>This method enables advanced distributed caching scenarios, including support for
        /// Redis backplane and distributed stampede protection, based on the provided configuration. It should be
        /// called during application startup as part of service registration.</remarks>
        /// <param name="action">An action to configure the FusionCache entry settings, such as expiration and jitter parameters.</param>
        /// <param name="distributedAction">An action to configure distributed cache settings, including instance name and connection details.</param>
        /// <param name="options">The options used to configure distributed FusionCache behavior. Cannot be null.</param>
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

            var redisOptions = GetRedisConnectionOptions(distributedSettings.InstanceName, distributedSettings.RedisConnectionString);
            var redisBackplaneOptions = GetRedisBackplaneConnectionOptions(distributedSettings.RedisConnectionString);

            var muxerRedisConnectionString = distributedSettings.MuxerRedisConnectionString;

            services.AddMemoryCache();

            if (fusionCacheEnabledOptions?.EnableBackplane ?? false)
            {
                if (fusionCacheEnabledOptions?.EnableDistributedStampedeProtection ?? false)
                {
                    if (fusionCacheEnabledOptions?.EnableLogging ?? false)
                    {
                        Task.FromResult(services.AddFusionCacheDistributedStampedeLoggingAsync(configuration, cacheOptions, muxerRedisConnectionString));
                    }
                    else
                    {
                        Task.FromResult(services.AddFusionCacheDistributedStampedeProtectionAsync(configuration, cacheOptions, muxerRedisConnectionString));
                    }
                }
                else
                {
                    services.AddFusionCacheWithBackplane(configuration, cacheOptions, redisOptions, redisBackplaneOptions);
                }
            }
            else
            {
                services.AddFusionCacheWithoutBackplane(configuration, cacheOptions, redisOptions);
            }

            return services;
        }

        #region "Internal Methods"

        /// <summary>
        /// Configures FusionCache with the specified entry settings and distributed cache options, using Redis as the
        /// distributed cache, without enabling a backplane.
        /// </summary>
        /// <remarks>This method sets up FusionCache with System.Text.Json serialization and Redis as the
        /// distributed cache, but does not configure a backplane for cross-instance cache synchronization. Use this
        /// method when distributed cache coordination is not required.</remarks>
        /// <param name="configuration">The cache entry settings to apply as defaults for FusionCache entries. Specifies durations, fail-safe
        /// behavior, eager refresh, and factory timeouts.</param>
        /// <param name="cacheOptions">The distributed cache options to use, including timeouts, background operation settings, and jitter
        /// configuration.</param>
        /// <param name="redisOptions">The Redis cache options used to configure the underlying distributed cache implementation.</param>
        /// <returns>The current IServiceCollection instance with FusionCache and Redis distributed cache services configured.</returns>
        internal IServiceCollection AddFusionCacheWithoutBackplane(FusionCacheEntrySettings configuration, FusionCacheDistributedOptions cacheOptions,
            RedisCacheOptions redisOptions)
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
                .WithDistributedCache(new RedisCache(redisOptions));

            return services;
        }

        /// <summary>
        /// Configures FusionCache with distributed Redis caching and Redis backplane support using the specified
        /// settings.
        /// </summary>
        /// <remarks>This method enables distributed caching and cross-instance cache synchronization for
        /// FusionCache using Redis. It applies the provided configuration and options to ensure consistent cache
        /// behavior and failover support. Use this method when you require both distributed cache and backplane
        /// features in a multi-instance environment.</remarks>
        /// <param name="configuration">The cache entry settings to apply as the default configuration for FusionCache entries. Specifies durations,
        /// fail-safe, eager refresh, and timeout options.</param>
        /// <param name="cacheOptions">The distributed cache options, including timeouts, circuit breaker, background operation settings, and
        /// jitter configuration for FusionCache.</param>
        /// <param name="redisOptions">The Redis cache options used to configure the distributed cache provider.</param>
        /// <param name="redisBackplaneOptions">The Redis backplane options used to configure the backplane for cache synchronization across instances.</param>
        /// <returns>The same IServiceCollection instance with FusionCache, distributed Redis cache, and Redis backplane services
        /// configured.</returns>
        internal IServiceCollection AddFusionCacheWithBackplane(FusionCacheEntrySettings configuration, FusionCacheDistributedOptions cacheOptions,
            RedisCacheOptions redisOptions, RedisBackplaneOptions redisBackplaneOptions)
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
                .WithDistributedCache(new RedisCache(redisOptions))
                // ADD BACKPLANE BASED ON REDIS
                .WithBackplane(new RedisBackplane(redisBackplaneOptions));

            return services;
        }

        /// <summary>
        /// Configures FusionCache with distributed stampede protection using Redis for distributed cache, backplane,
        /// and distributed locking, and applies the specified cache entry and distributed options asynchronously.
        /// </summary>
        /// <remarks>A single Redis connection multiplexer instance is created and shared across all
        /// FusionCache distributed components for optimal performance and resource management. This method should be
        /// called during service registration to ensure correct setup of distributed features.</remarks>
        /// <param name="configuration">The cache entry settings to apply as the default options for FusionCache entries. Specifies durations,
        /// fail-safe, eager refresh, and timeout behaviors.</param>
        /// <param name="cacheOptions">The distributed cache options to configure circuit breaker, timeouts, background operations, and jittering
        /// for FusionCache's distributed layer.</param>
        /// <param name="muxerRedisConnectionString">The connection string used to establish a shared Redis connection multiplexer for distributed cache,
        /// backplane, and distributed locking.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the service collection with
        /// FusionCache configured for distributed stampede protection.</returns>
        internal async Task<IServiceCollection> AddFusionCacheDistributedStampedeProtectionAsync(FusionCacheEntrySettings configuration,
            FusionCacheDistributedOptions cacheOptions, string muxerRedisConnectionString)
        {
            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            IConnectionMultiplexer muxer = await ConnectionMultiplexer.ConnectAsync(muxerRedisConnectionString);

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

        /// <summary>
        /// Configures FusionCache with distributed stampede logging, Redis-based distributed cache, backplane, and
        /// distributed locker, using the specified settings and connection string.
        /// </summary>
        /// <remarks>A single Redis connection multiplexer instance is created and shared across
        /// distributed cache, backplane, and distributed locker components for optimal resource usage. Logging levels
        /// and circuit breaker durations are set based on the provided options. This method is intended for internal
        /// use and should be called during service registration.</remarks>
        /// <param name="configuration">The cache entry settings to apply as the default options for FusionCache entries. Specifies durations,
        /// fail-safe behavior, eager refresh, and factory timeouts.</param>
        /// <param name="cacheOptions">The distributed cache options, including timeouts, background operation settings, jitter configuration, and
        /// logging levels.</param>
        /// <param name="muxerRedisConnectionString">The connection string used to establish a shared Redis connection multiplexer for distributed cache,
        /// backplane, and distributed locker integration.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the service collection with
        /// FusionCache and related distributed features configured.</returns>
        internal async Task<IServiceCollection> AddFusionCacheDistributedStampedeLoggingAsync(FusionCacheEntrySettings configuration,
            FusionCacheDistributedOptions cacheOptions, string muxerRedisConnectionString)
        {
            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            IConnectionMultiplexer muxer = await ConnectionMultiplexer.ConnectAsync(muxerRedisConnectionString);

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