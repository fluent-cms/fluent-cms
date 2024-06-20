using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;

namespace FluentCMS.Services;


public class EntityService(IDao dao, ISchemaService schemaService):IEntityService
{
    public async Task<EntityList?> GetAll(string entityName)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        return entity is null ? null: new EntityList
        {
            Items = await dao.Get(entity.All()),
            TotalRecords = await dao.Count(entity.All())
        };
    }

    public async Task<int?> Insert(string entityName, Record record)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        return entity is null ? null : await dao.Exec(entity.Insert(record));
    }

    public async Task<int?> Update(string entityName, Record item)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        return entity is null ? null : await dao.Exec(entity.Update(item));
    }

    public async Task<int?> Delete(string entityName, Record item)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        return entity is null ? null : await dao.Exec(entity.Delete(item));
    }

    public async Task<object?> GetOne(string entityName, string id)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        return entity is null ? null : await dao.GetOne(entity.One(id));
    }
}