namespace AppEngine.Caching.Options;

/// <summary>
/// Provides configuration options for jitter and circuit breaker durations used in distributed cache operations.
/// </summary>
/// <remarks>These options allow customization of timing behaviors to improve resilience and reduce contention in
/// distributed caching scenarios. Adjust the durations to tune how the cache handles transient failures and randomizes
/// operation timing.</remarks>
public class FusionCacheJitterOptions
{
    /// <summary>
    /// Gets or sets the duration for which the distributed cache circuit breaker remains open after a failure is
    /// detected.
    /// </summary>
    /// <remarks>This duration determines how long requests to the distributed cache are blocked before
    /// retrying after a circuit breaker event. Adjust this value based on the expected recovery time of the cache
    /// backend to balance availability and fault tolerance.</remarks>
    public TimeSpan DistributedCacheCircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the maximum duration for applying random jitter to retry delays.
    /// </summary>
    /// <remarks>Jitter helps to prevent synchronized retries in distributed systems by adding randomness to
    /// retry intervals. Adjust this value to control the upper bound of the random delay added to each retry
    /// attempt.</remarks>
    public TimeSpan JitterMaxDuration { get; set; } = TimeSpan.FromSeconds(2);
}