namespace AppEngine.Caching.Settings;

/// <summary>
/// Represents the configuration settings for a cache entry in FusionCache, including expiration, fail-safe behavior,
/// and factory operation timeouts.
/// </summary>
/// <remarks>Use this class to customize how individual cache entries behave with respect to duration, fail-safe
/// mechanisms, eager refresh thresholds, and factory operation timeouts. Adjusting these settings allows fine-tuning of
/// cache resilience, refresh strategies, and performance characteristics for different caching scenarios.</remarks>
public class FusionCacheEntrySettings
{
    /// <summary>
    /// Gets or sets the duration for the operation.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether fail-safe mode is enabled.
    /// </summary>
    public bool IsFailSafeEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum duration that the fail-safe mechanism remains active before it is automatically
    /// disabled.
    /// </summary>
    /// <remarks>Set this property to limit how long fail-safe mode can be engaged. Adjusting this value can
    /// help balance system resilience and recovery time.</remarks>
    public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Gets or sets the duration for which fail-safe throttling is applied after a failure is detected.
    /// </summary>
    /// <remarks>This duration determines how long the system will suppress further operations or retries
    /// following a fail-safe event. Adjust this value to control the cooldown period before normal processing
    /// resumes.</remarks>
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the threshold at which an eager refresh is triggered.
    /// </summary>
    /// <remarks>The value represents a ratio between 0 and 1. When the relevant metric reaches or exceeds
    /// this threshold, an eager refresh operation may be initiated. Set to null to disable eager refresh
    /// behavior.</remarks>
    public float? EagerRefreshThreshold { get; set; } = 0.9f;

    /// <summary>
    /// Gets or sets the soft timeout interval for factory operations.
    /// </summary>
    /// <remarks>This timeout determines how long a factory operation is allowed to run before it is
    /// considered to have exceeded the preferred execution time. The operation may continue running after this
    /// interval, but it may be subject to cancellation or warning depending on the implementation.</remarks>
    public TimeSpan FactorySoftTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the maximum duration allowed for factory operations before timing out.
    /// </summary>
    public TimeSpan FactoryHardTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);
}