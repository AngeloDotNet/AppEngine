namespace AppEngine.Caching.Options;

/// <summary>
/// Represents configuration options for enabling features in FusionCache, such as backplane integration, distributed
/// stampede protection, and logging.
/// </summary>
/// <remarks>Use this class to specify which advanced FusionCache features should be enabled for a given cache
/// instance. These options control behaviors related to distributed caching scenarios and diagnostics. All options are
/// disabled by default.</remarks>
public class FusionCacheEnabledOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the distributed cache backplane is enabled.
    /// </summary>
    /// <remarks>Enable this property to synchronize cache updates across multiple application instances using
    /// a backplane mechanism. This is typically required in scaled-out or load-balanced environments to ensure cache
    /// consistency.</remarks>
    public bool EnableBackplane { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether distributed stampede protection is enabled.
    /// </summary>
    /// <remarks>When enabled, the system coordinates across multiple instances to prevent multiple concurrent
    /// requests from triggering the same expensive operation. This is useful in distributed environments to reduce
    /// redundant processing and resource usage.</remarks>
    public bool EnableDistributedStampedeProtection { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the logging options used to configure logging behavior for the cache instance.
    /// </summary>
    /// <remarks>Specify this property to customize how cache operations are logged, including log levels and
    /// message formatting. If not set, default logging options are applied.</remarks>
    public FusionCacheLoggingOptions? LoggingOptions { get; set; }
}