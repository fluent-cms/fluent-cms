using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;

public abstract class KeyValueCacheBase<T>
{
    private readonly IMemoryCache _memoryCache;
    private readonly string _prefix;

    protected KeyValueCacheBase(IMemoryCache memoryCache, string prefix)
    {
        this._memoryCache = memoryCache;
        this._prefix = prefix;
    }

    protected string CacheKey(string key) => _prefix + key;

    public void Remove(string key) => _memoryCache.Remove(CacheKey(key));

    public bool TryGetValue<TValue>(string key, out TValue? value) => _memoryCache.TryGetValue(CacheKey(key), out value);

    protected abstract MemoryCacheEntryOptions SetEntryOptions();

    public void Replace(string key, T value)
    {
        var cacheEntryOptions = SetEntryOptions();
        _memoryCache.Set(CacheKey(key), value, cacheEntryOptions);
    }

    public async Task<T?> GetOrSet(string key, Func<Task<T>> factory)
    {
        return await _memoryCache.GetOrCreateAsync(CacheKey(key), async entry =>
        {
            entry.SetOptions(SetEntryOptions());
            return await factory();
        });
    }
}
