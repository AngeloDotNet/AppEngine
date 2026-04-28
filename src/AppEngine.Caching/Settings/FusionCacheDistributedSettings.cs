namespace AppEngine.Caching.Settings;

/// <summary>
/// Settings for distributed cache, such as Redis connection string.
/// </summary>
public class FusionCacheDistributedSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the Redis server.
    /// </summary>
    public string RedisConnectionString { get; set; } = string.Empty;
}