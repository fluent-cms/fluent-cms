using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Services;

public class ViewService(IDao dao, ISchemaService schemaService, IEntityService entityService) : IViewService
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
        
        Entity.InListOrDetail? scope = view.AttributeNames?.Length > 0
            ? null
            : Entity.InListOrDetail.InList;

        var query = entity.List(  view.Filters,view.Sorts, null, cursor,
            entity.GetAttributes(null, scope, view.AttributeNames));
        var items = await dao.Many(query);
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

        if (!Cursor.GenerateCursor(items, view.Sorts, out var first, out var last))
        {
            return null;
        }
        
        await AttachRelatedEntity(entity, view, scope, items);
        
        var result = new ViewResult
        {
            Items = items,
        };
        
        if (!string.IsNullOrWhiteSpace(cursor.First))
        {
            //click back button, always has next button
            result.Last = last;
            if (hasMore)
            {
                result.First =  first ;
            }
        }
        else
        {
            //click next button, always has back button
            if (!string.IsNullOrWhiteSpace(cursor.Last )) 
            {
                result.First = first;
            } // if last == "" and first =="", means first page, no need to go back
            if (hasMore)
            {
                result.Last = last;
            }
        }

        return result;
    }


    public async Task<IDictionary<string, object>[]?> Many(string viewName,
        Dictionary<string, StringValues> querystringDictionary)
    {
        var (ok,view, entity) = await ResolvedView(viewName, querystringDictionary);
        if (!ok)
        {
            return null;
        }
        Entity.InListOrDetail? scope = view.AttributeNames?.Length > 0
            ? null
            : Entity.InListOrDetail.InDetail;
        
        var query = entity.List(  view.Filters,view.Sorts, new Pagination{Limit = view.PageSize}, null,
            entity.GetAttributes(null, scope, view.AttributeNames));
        var items = await dao.Many(query);
        if (items is null)
        {
            return null;
        }

        await AttachRelatedEntity(entity, view, scope, items);
        return items;
    }

    public async Task<IDictionary<string, object>?> One(string viewName,
        Dictionary<string, StringValues> querystringDictionary)
    {
        var (ok, view, entity) = await ResolvedView(viewName, querystringDictionary);
        if (!ok)
        {
            return null;
        }
        Entity.InListOrDetail? scope = view.AttributeNames?.Length > 0
            ? Entity.InListOrDetail.InDetail
            : null;

        var query = entity.One(view.Filters, entity.GetAttributes(null, scope, view.AttributeNames));
        var item = await dao.One(query);
        if (item is null)
        {
            return null;
        }
        await AttachRelatedEntity(entity, view, scope, [item]);
        return item;
    }
    
    private async Task AttachRelatedEntity(Entity entity, View view, Entity.InListOrDetail? scope,
        IDictionary<string, object>[] items)
    {
        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, scope, view.AttributeNames))
        {
            await entityService.AttachLookup(attribute, items,
                entity1 => entity1.GetAttributes(null, Entity.InListOrDetail.InList, null));
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, scope, view.AttributeNames))
        {
            await entityService.AttachCrosstable(attribute, items,
                entity1 => entity1.GetAttributes(null, Entity.InListOrDetail.InList, null));
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