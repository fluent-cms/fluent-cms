using Microsoft.Extensions.Caching.Memory;
using AutoMapper;

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
public class ImmutableCache<T>(IMemoryCache memoryCache, int ttlSeconds, string prefix)
{
    readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.CreateMap<T, T>()).CreateMapper();
    string CacheKey(string key) => prefix + key;  
    public void Remove(string key)
    {
        memoryCache.Remove(CacheKey(key));
    }
    
    public async Task<T?> GetOrSet(string key,  Func<Task<T>> factory)
    {
        var item = await memoryCache.GetOrCreateAsync<T>(CacheKey(key), async (entry) =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(ttlSeconds);
            return await factory();
        });
        return item is null ? item : _mapper.Map<T>(item);// make a deep copy
    }
}