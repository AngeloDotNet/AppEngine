namespace AppEngine.Caching.Options;

/// <summary>
/// Represents configuration options for distributed cache operations in FusionCache.
/// </summary>
/// <remarks>This class provides settings that control timeouts, background operation behavior, and advanced
/// options for distributed cache integration. It is typically used to customize how FusionCache interacts with a
/// distributed cache provider.</remarks>
public class FusionCacheDistributedOptions
{
    /// <summary>
    /// Gets or sets the soft timeout duration for distributed cache operations.
    /// </summary>
    /// <remarks>If a cache operation exceeds this duration, it may be considered failed or fallback logic may
    /// be triggered, depending on the implementation. Adjust this value to balance responsiveness and cache
    /// reliability.</remarks>
    public TimeSpan DistributedCacheSoftTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum duration to wait for a distributed cache operation before considering it failed.
    /// </summary>
    /// <remarks>This timeout acts as a hard limit; operations exceeding this duration are treated as
    /// failures. Adjust this value based on expected cache response times and application requirements.</remarks>
    public TimeSpan DistributedCacheHardTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets a value indicating whether distributed cache operations are allowed to run in the background.
    /// </summary>
    public bool AllowBackgroundDistributedCacheOperations { get; set; } = true;

    /// <summary>
    /// Gets or sets the options used to configure jitter behavior for cache operations.
    /// </summary>
    /// <remarks>Jitter options can be used to randomize cache expiration or refresh timings, which helps
    /// prevent cache stampedes and thundering herd problems in distributed systems. If not set, default jitter behavior
    /// is applied.</remarks>
    public FusionCacheJitterOptions? JitterOptions { get; set; }

    /// <summary>
    /// Gets or sets the options used to configure FusionCache integration.
    /// </summary>
    public FusionCacheEnabledOptions? FusionCacheEnabledOptions { get; set; }

    /// <summary>
    /// Gets or sets the file system path used to store cached data.
    /// </summary>
    public string CachePath { get; set; } = string.Empty;
}