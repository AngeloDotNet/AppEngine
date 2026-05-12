using AppEngine.Caching.Enums;

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
    public bool EnableAllFusionCacheOptions { get; set; } = false;
    public bool EnableDefaultEntryOptions { get; set; } = false;
    public bool EnableSystemTextJsonSerializer { get; set; } = false;
    public List<FusionCacheEntryEnumOptions> EnabledEntryOptions { get; set; } = [];
    public DistributedCacheEnum DistributedCacheType { get; set; } = DistributedCacheEnum.Redis;
    public bool EnableDistributedCache { get; set; } = false;
    public bool EnableBackplane { get; set; } = false;
    public bool EnableDistributedLocker { get; set; } = false;

    //public bool EnableBackplane { get; set; } = false;
    //public bool EnableDistributedStampedeProtection { get; set; } = false;
    //public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the logging options used to configure logging behavior for the cache instance.
    /// </summary>
    /// <remarks>Specify this property to customize how cache operations are logged, including log levels and
    /// message formatting. If not set, default logging options are applied.</remarks>
    public FusionCacheLoggingOptions? LoggingOptions { get; set; }
}