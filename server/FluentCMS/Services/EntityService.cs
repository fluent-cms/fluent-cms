using System.Text.Json;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using LanguageExt;
using Attribute = FluentCMS.Models.Queries.Attribute;

namespace FluentCMS.Services;

public class EntityService(IDao dao, ISchemaService schemaService) : IEntityService
{
    public async Task<IDictionary<string,object>?> One(string entityName, string id)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        var record = await dao.One(entity.One(id, entity.GetAttributes(null, Entity.InListOrDetail.InDetail)));
        if (record is null)
        {
            return null;
        }

        foreach (var detailLookupsAttribute in entity.GetAttributes(DisplayType.lookup, Entity.InListOrDetail.InDetail))
        {
            await LoadLookup(detailLookupsAttribute, [record],
                lookupEntity => lookupEntity.GetAttributes(null, Entity.InListOrDetail.InDetail));
        }

        return record;
    }

    public async Task<RecordList?> List(string entityName)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        var query = entity.List(null, null, entity.GetAttributes(null, Entity.InListOrDetail.InList));
        var records = await dao.Many(query);
        if (records is null)
        {
            return null;
        }

        foreach (var listLookupsAttribute in entity.GetAttributes(DisplayType.lookup, Entity.InListOrDetail.InList))
        {
            await LoadLookup(listLookupsAttribute, records, lookupEntity => lookupEntity.AttributesForLookup());
        }

        return new RecordList
        {
            Items = records,
            TotalRecords = await dao.Count(query)
        };
    }

    public async Task<int?> Insert(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity.GetDatabaseTypeCaster);
        return await dao.Exec(entity.Insert(record));
    }

    public async Task<int?> Update(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity.GetDatabaseTypeCaster);
        return await dao.Exec(entity.Update(record));
    }

    public async Task<int?> Delete(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity.GetDatabaseTypeCaster);
        return await dao.Exec(entity.Delete(record));
    }

    private async Task LoadCrossJoinTable(Attribute attribute, IDictionary<string,object>[] items, Func<Entity, Attribute[]> getFields)
    {
        var ids = attribute.GetValues(items);
        if (ids.Length == 0)
        {
            return;
        }

        var cross = attribute.Crosstable;
        if (cross is null)
        {
            return;
        }

        var query = cross.Many(getFields(cross.TargetEntity), ids, true);
        var lookupRecords = await dao.Many(query);
        if (lookupRecords is null)
        {
            return;
        }

        foreach (var lookupRecord in lookupRecords)
        {
            var lookupId = lookupRecord[cross.FromAttribute.Field];
            foreach (var item in items.Where(local => local[attribute.Parent.PrimaryKey].Equals(lookupId)))
            {
                item[attribute.Field] = lookupRecord;
            }
        }
    }

    private async Task LoadLookup(Attribute lookupAttribute, IDictionary<string,object>[] items, Func<Entity, Attribute[]> getFields)
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
            foreach (var item in items.Where(local =>
                         local[lookupAttribute.Field] is not null && local[lookupAttribute.Field].Equals(lookupId)))
            {
                item[lookupAttribute.Field + "_data"] = lookupRecord;
            }
        }
    }

    private async Task<Attribute?> FindAttribute(string entityName, string attributeName)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        return entity.Attributes.FirstOrDefault(x => x.Field == attributeName);
    }

    public async Task<int?> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = await FindAttribute(entityName, attributeName);
        if (attribute is null || attribute.Crosstable is null)
        {
            return null;
        }

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, attribute.Crosstable.TargetEntity.GetDatabaseTypeCaster));
        return await dao.Exec(attribute.Crosstable.Delete(strId, items.ToArray()));
    }

    public async Task<int?> CrosstableSave(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = await FindAttribute(entityName, attributeName);
        if (attribute is null || attribute.Crosstable is null)
        {
            return null;
        }

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, attribute.Crosstable.TargetEntity.GetDatabaseTypeCaster));
        return await dao.Exec(attribute.Crosstable.Insert(strId,items.ToArray() ));
        }

    public async Task<RecordList?> CrosstableList(string entityName, string strId, string attributeName, bool exclude)
    {
        var attribute =await FindAttribute(entityName, attributeName);
        if (attribute is null || attribute.Crosstable is null)
        {
            return null;
        }
        
        var selectAttributes = attribute.Crosstable.TargetEntity.GetAttributes(null, Entity.InListOrDetail.InList);
        var query = attribute.Crosstable.Many(selectAttributes, attribute.CastToDatabaseType(strId), exclude);
        return new RecordList
        {
            Items = await dao.Many(query),
            TotalRecords = await dao.Count(query)
        };
    }
}