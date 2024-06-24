using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;

namespace FluentCMS.Services;

public class ViewService(IDao dao, IEntityService entityService, ISchemaService schemaService): IViewService
{
    public async Task<RecordList?> List(string viewName, Pagination? pagination)
    {
        var view = await schemaService.GetViewByName(viewName);
        if (view?.Entity is null)
        {
            return null;
        }

        var query = view.Entity.List(pagination, view.Sorts,view.Filters, view.GetAttributes());
        return new RecordList
        {
            Items =  await dao.Many(query)
        };
    }
}