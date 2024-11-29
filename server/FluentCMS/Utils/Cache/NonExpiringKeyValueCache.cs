using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;
public class NonExpiringKeyValueCache<T>(IMemoryCache memoryCache, string prefix)
    : KeyValueCacheBase<T>(memoryCache, prefix)
{
    protected override MemoryCacheEntryOptions SetEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove
        };
    }
}
