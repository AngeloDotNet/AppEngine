using AppEngine.Caching.Enums;
using AppEngine.Caching.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace AppEngine.Caching.Options;

public class CacheOptions
{
    internal bool EnableOptions { get; set; }
    internal bool EnableDefaultEntryOptions { get; set; }
    internal bool EnableDistributedCache { get; set; }
    internal bool EnableBackplane { get; set; }
    internal bool EnableDistributedLocker { get; set; }
    internal List<FusionCacheEntryEnumOptions> ListEntryOptions { get; set; } = [];
    internal bool EnableSystemTextJsonSerializer { get; set; }
    internal FusionCacheEntrySettings Configuration { get; set; } = new FusionCacheEntrySettings();
    internal FusionCacheDistributedOptions CacheDistributedOptions { get; set; } = new FusionCacheDistributedOptions();
    internal RedisCacheOptions RedisCacheOptions { get; set; } = new RedisCacheOptions();
    internal RedisBackplaneOptions RedisBackplaneOptions { get; set; } = new RedisBackplaneOptions();
    internal string MuxerRedisConnectionString { get; set; } = string.Empty;
    internal DistributedCacheEnum TypeDistributedCache { get; set; }
}