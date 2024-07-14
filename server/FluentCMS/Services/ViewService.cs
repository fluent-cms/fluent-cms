using Utils.KateQueryExecutor;
using Utils.QueryBuilder;

using Microsoft.Extensions.Primitives;

namespace FluentCMS.Services;


public class ViewService(KateQueryExecutor queryKateQueryExecutor, ISchemaService schemaService, IEntityService entityService) : IViewService
{
    public async Task<ViewResult?> List(string viewName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary)
    {
        var (ok, view, entity) = await ResolvedView(viewName, querystringDictionary);
        if (!ok)
        {
            return null;
        }

        if (cursor.Limit == 0 || cursor.Limit > view.PageSize)
        {
            cursor.Limit = view.PageSize;
        }
        cursor.Limit += 1;
        

        var query = entity.List(  view.Filters,view.Sorts, null, cursor, view.LocalAttributes(InListOrDetail.InList));
        var items = await queryKateQueryExecutor.Many(query);
        if (items is null)
        {
            return null;
        }
        
        var hasMore = items.Length == cursor.Limit;
        if (hasMore)
        {
            items = cursor.First != ""
                ? items.Skip(1).Take(items.Length -1).ToArray()
                : items.Take(items.Length -1).ToArray();
        }

        if (!cursor.GetFirstAndLastCursor(items, view.Sorts, hasMore,  
                out var first, out var hasPrevious,
                out var last, out var hasNext))
        {
            return null;
        }
        await AttachRelatedEntity(entity, view, InListOrDetail.InList, items);
        
        return new ViewResult
        {
            Items = items,
            First = first,
            HasPrevious = hasPrevious,
            Last = last,
            HasNext = hasNext
        };
    }


    public async Task<Record[]?> Many(string viewName, Dictionary<string, StringValues> querystringDictionary)
    {
        var (ok,view, entity) = await ResolvedView(viewName, querystringDictionary);
        if (!ok)
        {
            return null;
        }
        
        var query = entity.List(  view.Filters,view.Sorts, new Pagination{Limit = view.PageSize}, null,
            view.LocalAttributes(InListOrDetail.InDetail));
        var items = await queryKateQueryExecutor.Many(query);
        if (items is null)
        {
            return null;
        }

        await AttachRelatedEntity(entity, view, InListOrDetail.InDetail, items);
        return items;
    }

    public async Task<IDictionary<string, object>?> One(string viewName, Dictionary<string, StringValues> querystringDictionary)
    {
        var (ok, view, entity) = await ResolvedView(viewName, querystringDictionary);
        if (!ok)
        {
            return null;
        }

        var query = entity.One(view.Filters, view.LocalAttributes(InListOrDetail.InDetail));
        var item = await queryKateQueryExecutor.One(query);
        if (item is null)
        {
            return null;
        }
        await AttachRelatedEntity(entity, view, InListOrDetail.InDetail, [item]);
        return item;
    }
    
    private async Task AttachRelatedEntity(Entity entity, View view, InListOrDetail scope, Record[] items)
    {
        foreach (var attribute in view.GetAttributesByType(DisplayType.lookup, scope))
        {
            await entityService.AttachLookup(attribute, items,
                entity1 => entity1.LocalAttributes(scope));
        }

        foreach (var attribute in view.GetAttributesByType(DisplayType.crosstable, scope ))
        {
            await entityService.AttachCrosstable(attribute, items,
                entity1 => entity1.LocalAttributes(scope));
        }
    }
    
    private async Task<(bool,View, Entity)> ResolvedView(string viewName,
        Dictionary<string, StringValues> querystringDictionary)
    {
        var view = await schemaService.GetViewByName(viewName);
        var entity = view?.Entity;
        if (view is null || entity is null)
        {
            return (false, new View(), new Entity());
        }
        view.Filters?.Resolve(entity, querystringDictionary, null);
        return (true,view, entity);
    }
}