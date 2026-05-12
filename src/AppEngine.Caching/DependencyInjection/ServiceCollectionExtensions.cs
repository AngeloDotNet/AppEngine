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
    extension(IServiceCollection services)
    {
        //public async Task<IServiceCollection> AddCustomFusionCacheAsync(Action<FusionCacheEntrySettings> action,
        //    Action<FusionCacheDistributedSettings> distributedAction, IOptions<FusionCacheDistributedOptions> options)
        public async Task<IServiceCollection> AddCustomFusionCacheAsync(IConfiguration configuration)
        {
            var options = configuration.GetSection("AppEngine.Caching:CacheOptions").Get<CacheOptions>()
                ?? throw new InvalidOperationException("CacheOptions is null. Ensure that the configuration is properly set up.");

            //ArgumentNullException.ThrowIfNull(services);
            //ArgumentNullException.ThrowIfNull(action);

            //ArgumentNullException.ThrowIfNull(distributedAction);
            //ArgumentNullException.ThrowIfNull(options);

            //var configuration = new FusionCacheEntrySettings();
            //action.Invoke(configuration);

            //var distributedSettings = new FusionCacheDistributedSettings();
            //distributedAction.Invoke(distributedSettings);

            //services.TryAddTransient(_ => configuration);
            //services.TryAddTransient(_ => distributedSettings);

            //var cacheOptions = options.Value;
            //var fusionCacheEnabledOptions = cacheOptions.FusionCacheEnabledOptions;

            //var redisOptions = ServiceCollectionOptions.GetRedisConnectionOptions(distributedSettings.InstanceName, distributedSettings.RedisConnectionString);
            //var redisBackplaneOptions = ServiceCollectionOptions.GetRedisBackplaneConnectionOptions(distributedSettings.RedisConnectionString);

            //var muxerRedisConnectionString = distributedSettings.MuxerRedisConnectionString;

            //if (fusionCacheEnabledOptions is null)
            //{
            //    throw new InvalidOperationException("FusionCacheEnabledOptions is null. Ensure that the options are properly configured.");
            //}

            //var enableOptions = fusionCacheEnabledOptions.EnableAllFusionCacheOptions;
            //var enableDefaultEntryOptions = fusionCacheEnabledOptions.EnableDefaultEntryOptions;
            //var enableSystemTextJsonSerializer = fusionCacheEnabledOptions.EnableSystemTextJsonSerializer;

            //var listEntryOptions = fusionCacheEnabledOptions.EnabledEntryOptions;
            //var typeDistributedCache = fusionCacheEnabledOptions.DistributedCacheType;

            //var enableDistributedCache = fusionCacheEnabledOptions.EnableDistributedCache;
            //var enableBackplane = fusionCacheEnabledOptions.EnableBackplane;
            //var enableDistributedLocker = fusionCacheEnabledOptions.EnableDistributedLocker;

            services.AddMemoryCache();

            var optionsFusionCache = new CacheOptions
            {
                EnableOptions = options.EnableOptions,
                EnableDefaultEntryOptions = options.EnableDefaultEntryOptions,
                EnableDistributedCache = options.EnableDistributedCache,
                EnableBackplane = options.EnableBackplane,
                EnableDistributedLocker = options.EnableDistributedLocker,
                ListEntryOptions = options.ListEntryOptions,
                EnableSystemTextJsonSerializer = options.EnableSystemTextJsonSerializer,
                Configuration = options.Configuration,
                CacheDistributedOptions = options.CacheDistributedOptions,
                RedisOptions = options.RedisOptions,
                RedisBackplaneOptions = options.RedisBackplaneOptions,
                MuxerRedisConnectionString = options.MuxerRedisConnectionString,
                TypeDistributedCache = options.TypeDistributedCache
            };

            //await services.AddFusionCacheWithOptionsAsync(enableOptions, enableDefaultEntryOptions, enableDistributedCache, enableBackplane,
            //    enableDistributedLocker, listEntryOptions, enableSystemTextJsonSerializer, configuration, cacheOptions, redisOptions,
            //    redisBackplaneOptions, muxerRedisConnectionString, typeDistributedCache);

            await services.AddFusionCacheWithOptionsAsync(optionsFusionCache);

            // TODO:
            // - Cache Levels (Distributed, Local, Hybrid)
            // - Cache Stampede protection with Redis (with and without backplane)
            // - Conditional refresh
            // - Tagging support

            return services;
        }

        #region "Internal Methods"

        internal async Task<IServiceCollection> AddFusionCacheWithOptionsAsync(CacheOptions options)
        //internal async Task<IServiceCollection> AddFusionCacheWithOptionsAsync(bool enableOptions, bool enableDefaultEntryOptions,
        //    bool enableDistributedCache, bool enableBackplane, bool enableDistributedLocker, List<FusionCacheEntryEnumOptions> listEntryOptions,
        //    bool enableSystemTextJsonSerializer, FusionCacheEntrySettings configuration, FusionCacheDistributedOptions cacheOptions,
        //    RedisCacheOptions redisOptions, RedisBackplaneOptions redisBackplaneOptions, string muxerRedisConnectionString,
        //    DistributedCacheEnum typeDistributedCache)
        {
            var builder = services.AddFusionCache();

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
            IConnectionMultiplexer? muxer = null;

            if (muxer is not null)
            {
                muxer = await ConnectionMultiplexer.ConnectAsync(options.MuxerRedisConnectionString);
            }

            if (options.EnableDistributedCache)
            {
                builder.AddDistributedCache(options.CacheDistributedOptions, options.RedisOptions, options.TypeDistributedCache, muxer);
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

        #endregion
    }
}