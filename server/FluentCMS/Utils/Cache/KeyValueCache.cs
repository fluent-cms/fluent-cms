using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;
public sealed class KeyValueCache<T>(IServiceProvider provider, string prefix, int ttlSeconds) 
{
    private string CacheKey(string key) => prefix + key;

    private readonly ICacheProvider _cacheProvider = provider.GetService<HybridCache>() switch
    {
        { } hybridCache => new HybridCacheProvider(hybridCache),
        _ => provider.GetService<IMemoryCache>() switch
        {
            { } memoryCache => new MemoryCacheProvider(memoryCache),
            _ => throw new Exception("failed to get cache provider"),
        }
    };

    public ValueTask Remove(string key,CancellationToken ct = default) => _cacheProvider.Remove(CacheKey(key), ct);

    public  ValueTask<T> GetOrSet(string key, Func<CancellationToken,ValueTask<T>> factory, CancellationToken ct = default)
        => _cacheProvider.GetOrCreate(CacheKey(key),ttlSeconds,factory, ct);
}
