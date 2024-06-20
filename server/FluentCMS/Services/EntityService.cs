using FluentCMS.Utils.Dao;

namespace FluentCMS.Services;


public class EntityService(IDao dao, ISchemaService schemaService):IEntityService
{
    public async Task<EntityList?> GetAll(string entityName)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        return new EntityList
        {
            Items = await dao.Get(entity.All()),
            TotalRecords = await dao.Count(entity.All())
        };
    }
}