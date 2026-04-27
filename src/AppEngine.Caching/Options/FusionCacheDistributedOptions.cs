using Microsoft.Extensions.Logging;

namespace AppEngine.Caching.Options;

public class FusionCacheDistributedOptions
{
    public TimeSpan DistributedCacheSoftTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan DistributedCacheHardTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public bool AllowBackgroundDistributedCacheOperations { get; set; } = true;
    public FusionCacheJitterOptions? JitterOptions { get; set; }
    public FusionCacheEnabledOptions? FusionCacheEnabledOptions { get; set; }
}

public class FusionCacheJitterOptions
{
    public TimeSpan DistributedCacheCircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan JitterMaxDuration { get; set; } = TimeSpan.FromSeconds(2);
}

public class FusionCacheEnabledOptions
{
    public bool EnableBackplane { get; set; } = false;
    public bool EnableDistributedStampedeProtection { get; set; } = false;
    public bool EnableLogging { get; set; } = false;
    public FusionCacheLoggingOptions? LoggingOptions { get; set; }
}

public class FusionCacheLoggingOptions
{
    public LogLevel FailSafeActivationLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel SerializationErrorsLogLevel { get; set; } = LogLevel.Warning;

    public LogLevel DistributedCacheSyntheticTimeoutsLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel DistributedCacheErrorsLogLevel { get; set; } = LogLevel.Error;

    public LogLevel FactorySyntheticTimeoutsLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel FactoryErrorsLogLevel { get; set; } = LogLevel.Error;
}