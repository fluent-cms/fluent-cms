using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;

public abstract class KeyValueCacheBase<T>(IMemoryCache memoryCache, string prefix)
{
    private string CacheKey(string key) => prefix + key;

    public void Remove(string key) => memoryCache.Remove(CacheKey(key));

    public bool TryGetValue(string key, out T? value) => memoryCache.TryGetValue(CacheKey(key), out value);

    protected abstract MemoryCacheEntryOptions SetEntryOptions();

    public void Replace(string key, T value)
    {
        var cacheEntryOptions = SetEntryOptions();
        memoryCache.Set(CacheKey(key), value, cacheEntryOptions);
    }

    public async Task<T?> GetOrSet(string key, Func<Task<T>> factory)
    {
        return await memoryCache.GetOrCreateAsync(CacheKey(key), async entry =>
        {
            entry.SetOptions(SetEntryOptions());
            return await factory();
        });
    }
}
