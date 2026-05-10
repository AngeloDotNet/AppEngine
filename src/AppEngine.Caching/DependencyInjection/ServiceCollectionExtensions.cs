using AppEngine.Caching.Enums;
using AppEngine.Caching.Options;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NeoSmart.Caching.Sqlite;
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
        public IServiceCollection AddCustomFusionCache(Action<FusionCacheEntrySettings> action, Action<FusionCacheDistributedSettings> distributedAction,
            IOptions<FusionCacheDistributedOptions> options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(action);

            ArgumentNullException.ThrowIfNull(distributedAction);
            ArgumentNullException.ThrowIfNull(options);

            var configuration = new FusionCacheEntrySettings();
            action.Invoke(configuration);

            var distributedSettings = new FusionCacheDistributedSettings();
            distributedAction.Invoke(distributedSettings);

            services.TryAddTransient(_ => configuration);
            services.TryAddTransient(_ => distributedSettings);

            var cacheOptions = options.Value;
            var fusionCacheEnabledOptions = cacheOptions.FusionCacheEnabledOptions;

            var redisOptions = ServiceCollectionOptions.GetRedisConnectionOptions(distributedSettings.InstanceName, distributedSettings.RedisConnectionString);
            var redisBackplaneOptions = ServiceCollectionOptions.GetRedisBackplaneConnectionOptions(distributedSettings.RedisConnectionString);

            var muxerRedisConnectionString = distributedSettings.MuxerRedisConnectionString;

            services.AddMemoryCache();

            // TODO => Refactor this method to use the new AddFusionCacheWithOptionsAsync internal method for better modularity and maintainability.
            //services.AddFusionCacheWithOptionsAsync();

            return services;
        }

        #region "Internal Methods"

        internal async Task<IServiceCollection> AddFusionCacheWithOptionsAsync(FusionCacheEntrySettings configuration,
            RedisCacheOptions redisOptions, RedisBackplaneOptions redisBackplaneOptions, FusionCacheDistributedOptions cacheOptions,
            string muxerRedisConnectionString, DistributedCacheEnum typeDistributedCache)
        {
            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            //IConnectionMultiplexer muxer = await ConnectionMultiplexer.ConnectAsync(muxerRedisConnectionString);

            //var logOptions = cacheOptions.FusionCacheEnabledOptions?.LoggingOptions;

            var builder = services.AddFusionCache();

            var EnableOptions = true;
            var EnableDefaultEntryOptions = true;
            var EnableSystemTextJsonSerializer = true;

            if (EnableOptions)
            {
                builder.AddOptions(cacheOptions, FusionCacheEnumOptions.EnableAllFusionCacheOptions);
            }

            var listEntryOptions = new List<FusionCacheEntryEnumOptions>();

            if (EnableDefaultEntryOptions)
            {
                builder.AddDefaultEntryOptions(cacheOptions, configuration, listEntryOptions);
            }

            if (EnableSystemTextJsonSerializer)
            {
                builder.WithSerializer(new FusionCacheSystemTextJsonSerializer());
            }

            var EnableDistributedCache = true;
            var UseRedisMuxer = true;

            // Create a single instance of ConnectionMultiplexer to be shared across the application
            // This is important for performance and resource management when using StackExchange.Redis (Optimize Redis Usage)
            IConnectionMultiplexer? muxer = null;

            if (UseRedisMuxer)
            {
                muxer = await ConnectionMultiplexer.ConnectAsync(muxerRedisConnectionString);
            }

            if (EnableDistributedCache)
            {
                if (typeDistributedCache == DistributedCacheEnum.Redis)
                {
                    if (muxer is null)
                    {
                        throw new InvalidOperationException("ConnectionMultiplexer instance is null. Ensure that the Redis connection is properly established.");
                    }

                    if (UseRedisMuxer)
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
            }

            var EnableBackplane = true;

            if (EnableBackplane)
            {
                if (muxer is null)
                {
                    throw new InvalidOperationException("ConnectionMultiplexer instance is null. Ensure that the Redis connection is properly established.");
                }

                builder.WithBackplane(new RedisBackplane(new RedisBackplaneOptions()
                {
                    ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                }));
            }
            else
            {
                builder.WithBackplane(new RedisBackplane(redisBackplaneOptions));
            }

            var EnableDistributedLocker = true;

            if (EnableDistributedLocker)
            {
                if (muxer is null)
                {
                    throw new InvalidOperationException("ConnectionMultiplexer instance is null. Ensure that the Redis connection is properly established.");
                }

                if (UseRedisMuxer)
                {
                    builder.WithDistributedLocker(new RedisDistributedLocker(new RedisDistributedLockerOptions
                    {
                        ConnectionMultiplexerFactory = () => Task.FromResult(muxer)
                    }));
                }
                else
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
            }

            return builder.Services;
        }

        // - TODO => Cache Levels (Distributed, Local, Hybrid)
        // - TODO => Cache Stampede protection with Redis (with and without backplane)
        // - TODO => Conditional refresh
        // - OK => Disc cache with SQLite or similar
        // - TODO => Tagging support

        #endregion
    }
}