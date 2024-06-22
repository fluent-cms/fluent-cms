using System.Text.Json;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using LanguageExt;
using Attribute = FluentCMS.Models.Queries.Attribute;

namespace FluentCMS.Services;
using Record = IDictionary<string,object>;


public class EntityService(IDao dao, ISchemaService schemaService):IEntityService
{
    private async Task LoadLookup(Attribute lookupAttribute, Record[] items, Func<Entity, Attribute[]> getFields)
    {
        var lookupEntity = await schemaService.GetEntityByName(lookupAttribute.GetLookupEntityName());
        if (lookupEntity is null)
        {
            return;
        }
        
        var ids = lookupAttribute.GetValues(items);
        if (ids.Length == 0)
        {
            return;
        }
        var lookupRecords = await dao.Many(lookupEntity.Many(ids, getFields(lookupEntity)));
        if (lookupRecords is null)
        {
            return;
        }
        
        foreach (var lookupRecord in lookupRecords)
        {
            var lookupId = lookupRecord[lookupEntity.PrimaryKey];
            foreach (var item in items.Where(local => local[lookupAttribute.Field] is not null && local[lookupAttribute.Field].Equals(lookupId)))
            {
                item[lookupAttribute.Field + "_data"] = lookupRecord;
            }
        }
    }
    
    public async Task<object?> One(string entityName, string id)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }
        var record =await dao.One(entity.One(id));
        if (record is null)
        {
            return null;
        }

        foreach (var detailLookupsAttribute in entity.DetailLookups())
        {
            await LoadLookup(detailLookupsAttribute, [record], lookupEntity => lookupEntity.ListAttributes());
        }

        return record;
    }

    public async Task<EntityList?> List(string entityName)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }
        var records = await dao.Many(entity.List());
        if (records is null)
        {
            return null;
        }

        foreach (var listLookupsAttribute in entity.ListLookups())
        {
            await LoadLookup(listLookupsAttribute, records, lookupEntity => lookupEntity.AttributesForLookup());
        }
        return new EntityList
        {
            Items = records,
            TotalRecords = await dao.Count(entity.List())
        };
    }

    public async Task<int?> Insert(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record =  RecordParser.Parse(ele, entity.GetDetailFieldParser);
        return  await dao.Exec(entity.Insert(record));
    }

    public async Task<int?> Update(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;
        
        var record =  RecordParser.Parse(ele, entity.GetDetailFieldParser);
        return await dao.Exec(entity.Update(record));
    }

    public async Task<int?> Delete(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;
        
        var record =  RecordParser.Parse(ele, entity.GetDetailFieldParser);
        return await dao.Exec(entity.Delete(record));
    }

    
}