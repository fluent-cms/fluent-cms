using Microsoft.Extensions.Caching.Hybrid;

namespace FluentCMS.Utils.Cache;

public sealed class HybridCacheProvider(HybridCache hybridCache):ICacheProvider
{
    public ValueTask<T> GetOrCreate<T>(string key,int ttlSec, Func<CancellationToken, ValueTask<T>> factory,  CancellationToken ct = default)
    {
        var options = new HybridCacheEntryOptions
        {
            LocalCacheExpiration = TimeSpan.FromSeconds(ttlSec /10),
            Expiration = TimeSpan.FromSeconds(ttlSec),
        };
        return hybridCache.GetOrCreateAsync(key,factory,options,null,ct);
    }

    public ValueTask Remove(string key, CancellationToken ct = default) => hybridCache.RemoveAsync(key, ct); 
}