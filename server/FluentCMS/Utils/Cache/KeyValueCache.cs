using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;
public sealed class KeyValueCache<T>
{
    private string CacheKey(string key) => _prefix + key;

    private readonly ICacheProvider _cacheProvider;

    private readonly string _prefix;
    private readonly TimeSpan _expiration;
    private readonly TimeSpan _localCacheExpiration;

    public KeyValueCache(IServiceProvider provider, ILogger<KeyValueCache<T>> logger, string prefix, int ttlSeconds)
    {
        _prefix = prefix;
        _expiration = TimeSpan.FromSeconds(Math.Min(1, ttlSeconds));
        _localCacheExpiration = TimeSpan.FromSeconds(Math.Min(1, ttlSeconds / 3));

        if (provider.GetService<HybridCache>() is { } hybridCache)
        {
            logger.LogInformation($"""
                                  ***********************************************
                                  Prefix = {prefix}, using hybrid cache, TTL = {ttlSeconds}
                                  ***********************************************
                                  """);
            _cacheProvider = new HybridCacheProvider(hybridCache);
            return;
        }

        if (provider.GetService<IMemoryCache>() is { } memoryCache)
        {
            logger.LogInformation($"""
                                   ***********************************************
                                   Prefix = {prefix}, using memory cache, TTL = {ttlSeconds}
                                   ***********************************************
                                   """);
             
            _cacheProvider = new MemoryCacheProvider(memoryCache);
            return;
        }

        throw new Exception("failed to get cache provider");
    }

    public ValueTask Remove(string key,CancellationToken ct = default) => _cacheProvider.Remove(CacheKey(key), ct);

    public  ValueTask<T> GetOrSet(string key, Func<CancellationToken,ValueTask<T>> factory, CancellationToken ct = default)
        => _cacheProvider.GetOrCreate(CacheKey(key),_expiration,_localCacheExpiration,factory, ct);
}
