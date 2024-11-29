using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;
public class ExpiringKeyValueCache<T>(IMemoryCache memoryCache, int ttlSeconds, string prefix)
    : KeyValueCacheBase<T>(memoryCache, prefix)
{
    protected override MemoryCacheEntryOptions SetEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
        };
    }
}
