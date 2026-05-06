namespace AppEngine.Caching.Settings;

/// <summary>
/// Settings for distributed cache, such as Redis connection string.
/// </summary>
public class FusionCacheDistributedSettings
{
    /// <summary>
    /// Gets or sets the name of the current instance.
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string used to connect to the Redis server.
    /// </summary>
    public string RedisConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string used to configure the Redis multiplexer.
    /// </summary>
    public string MuxerRedisConnectionString { get; set; } = string.Empty;
}