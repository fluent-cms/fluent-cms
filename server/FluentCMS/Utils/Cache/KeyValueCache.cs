using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.Utils.Cache;

/*
add this to DI
private async Task<Query> GetQuery(string viewName, CancellationToken cancellationToken)
{
    var view = await viewCache.GetOrSet(viewName,
    async () => await querySchemaService.GetByName(viewName, cancellationToken));
    return view;
}
*/


//asp.net core is going to supports hybrid cache, use memory cache only for now
//https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-8.0
//https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid?view=aspnetcore-9.0
public class KeyValueCache<T>(IMemoryCache memoryCache, int ttlSeconds, string prefix)
{
    string CacheKey(string key) => prefix + key;  
    public void Remove(string key)
    {
        memoryCache.Remove(CacheKey(key));
    }
    
    public async Task<T?> GetOrSet(string key,  Func<Task<T>> factory)
    {
        return await memoryCache.GetOrCreateAsync<T>(CacheKey(key), async (entry) =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(ttlSeconds);
            return await factory();
        });
    }
}