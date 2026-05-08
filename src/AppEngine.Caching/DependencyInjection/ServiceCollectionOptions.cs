using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace AppEngine.Caching.DependencyInjection;

public static class ServiceCollectionOptions
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
}