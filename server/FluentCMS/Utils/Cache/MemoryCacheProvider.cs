using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;

public class MemoryCacheProvider(IMemoryCache memoryCache) : ICacheProvider
{
    public async ValueTask<T> GetOrCreate<T>(string key, int ttlSec, Func<CancellationToken, ValueTask<T>> factory, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSec)
        };
        var val = await memoryCache.GetOrCreateAsync<T>(key, async entry => await factory(ct),options);
        return val!;
    }

    public ValueTask Remove(string key, CancellationToken ct = default)
    {
        memoryCache.Remove(key);
        return ValueTask.CompletedTask;
    }
}