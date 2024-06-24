using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;

namespace FluentCMS.Services;

public class ViewService(IDao dao, ISchemaService schemaService, IEntityService entityService) : IViewService
{
    public async Task<RecordList?> List(string viewName, Pagination? pagination)
    {
        var view = await schemaService.GetViewByName(viewName);
        var entity = view?.Entity;
        if (view is null || entity is null)
        {
            return null;
        }

        pagination ??= new Pagination
        {
            Rows = view.PageSize,
        };

        Entity.InListOrDetail? scope = view.AttributeNames?.Length > 0 ? null : Entity.InListOrDetail.InList; 
        var query = entity.List(pagination, view.Sorts, view.Filters, entity.GetAttributes(null, scope, view.AttributeNames));
        var items = await dao.Many(query);
        if (items is null)
        {
            return null;
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, scope, view.AttributeNames))
        {
            await entityService.AttachLookup(attribute, items,
                entity1 => entity1.GetAttributes(null, Entity.InListOrDetail.InList, null));
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, scope, view.AttributeNames))
        {
            await entityService.AttachCrosstable(attribute, items,
                entity1 => entity1.GetAttributes(null, Entity.InListOrDetail.InList,null));
        }

        return new RecordList
        {
            Items = items,
            HasMore = items.Length == view.PageSize,
            Cursor = Pagination.GenerateCursor(items, view.Sorts)
        };
    }
}