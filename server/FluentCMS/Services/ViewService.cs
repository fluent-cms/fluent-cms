using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.Cache;
namespace FluentCMS.Services;

using static InvalidParamExceptionFactory;

public class ViewService(
    KateQueryExecutor kateQueryExecutor, 
    ISchemaService schemaService, 
    IEntityService entityService,
    KeyValCache<View> viewCache
    ) : IViewService
{
    public async Task<ViewResult> List(string viewName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary 
        )
    {
        var view = await ResolvedView(viewName, querystringDictionary);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");
        if (cursor.Limit == 0 || cursor.Limit > view.PageSize)
        {
            cursor.Limit = view.PageSize;
        }
        cursor.Limit += 1;
        var query = entity.ListQuery(view.Filters,view.Sorts, null, cursor, CheckResult(view.LocalAttributes(InListOrDetail.InList)));
        var items = await kateQueryExecutor.Many(query);
        
        var hasMore = items.Length == cursor.Limit;
        if (hasMore)
        {
            items = cursor.First != ""
                ? items.Skip(1).Take(items.Length -1).ToArray()
                : items.Take(items.Length -1).ToArray();
        }

        var nextCursor = CheckResult(cursor.GetNextCursor(items, view.Sorts, hasMore));
        await AttachRelatedEntity(view, InListOrDetail.InList, items);
        
        return new ViewResult
        {
            Items = items,
            First = nextCursor.First,
            HasPrevious = !string.IsNullOrWhiteSpace(nextCursor.First),
            Last = nextCursor.Last,
            HasNext = !string.IsNullOrWhiteSpace(nextCursor.Last)
        };
    }


    public async Task<Record[]> Many(string viewName, Dictionary<string, StringValues> querystringDictionary)
    {
        var view = await ResolvedView(viewName, querystringDictionary);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");
       
        var query = entity.ListQuery(  view.Filters,view.Sorts, new Pagination{Limit = view.PageSize}, null,
            CheckResult(view.LocalAttributes(InListOrDetail.InDetail)));
        var items = await kateQueryExecutor.Many(query);
        await AttachRelatedEntity(view, InListOrDetail.InDetail, items);
        return items;
    }

    public async Task<Record> One(string viewName, Dictionary<string, StringValues> querystringDictionary)
    {
        var view = await ResolvedView(viewName, querystringDictionary);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");

        var attributes = CheckResult(view.LocalAttributes(InListOrDetail.InDetail));
        var query = CheckResult(entity.OneQuery(view.Filters, attributes ));
        var item = NotNull(await kateQueryExecutor.One(query))
            .ValOrThrow("Not find record");
        await AttachRelatedEntity(view, InListOrDetail.InDetail, [item]);
        return item;
    }
    
    private async Task AttachRelatedEntity(View view, InListOrDetail scope, Record[] items)
    {
        foreach (var attribute in CheckResult(view.GetAttributesByType(DisplayType.lookup, scope)))
        {
            await entityService.AttachLookup(attribute, items,
                entity1 => entity1.LocalAttributes(scope));
        }

        foreach (var attribute in CheckResult(view.GetAttributesByType(DisplayType.crosstable, scope )))
        {
            await entityService.AttachCrosstable(attribute, items,
                entity1 => entity1.LocalAttributes(scope));
        }
    }
    
    private async Task<View> ResolvedView(string viewName,
        Dictionary<string, StringValues> querystringDictionary)
    {
        var view = await viewCache.GetOrSet(viewName, async () => await schemaService.GetViewByName(viewName));
        CheckResult(view.Filters?.Resolve(view.Entity!, querystringDictionary, null));
        return view;
    }
}