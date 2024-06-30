using System.Text.Json;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using Attribute = FluentCMS.Models.Queries.Attribute;

namespace FluentCMS.Services;

public class EntityService(IDao dao, ISchemaService schemaService) : IEntityService
{
    public async Task<Record?> One(string entityName, string id)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        var record = await dao.One(entity.ById(id, entity.GetAttributes(null, Entity.InListOrDetail.InDetail, null)));
        if (record is null)
        {
            return null;
        }

        foreach (var detailLookupsAttribute in entity.GetAttributes(DisplayType.lookup, Entity.InListOrDetail.InDetail,null))
        {
            await AttachLookup(detailLookupsAttribute, [record],
                lookupEntity => lookupEntity.GetAttributes(null, Entity.InListOrDetail.InDetail,null));
        }

        return record;
    }

    public async Task<ListResult?> List(string entityName, Pagination? pagination, Sorts? sorts, Filters? filters)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null)
        {
            return null;
        }

        pagination ??= new Pagination
        {
            Limit = entity.DefaultPageSize
        };

        var query = entity.List(filters, sorts, pagination, null,
            entity.GetAttributes(null, Entity.InListOrDetail.InList, null));
        var records = await dao.Many(query);
        if (records is null)
        {
            return null;
        }

        foreach (var listLookupsAttribute in entity.GetAttributes(DisplayType.lookup, Entity.InListOrDetail.InList, null))
        {
            await AttachLookup(listLookupsAttribute, records, lookupEntity => lookupEntity.AttributesForLookup());
        }

        return new ListResult
        {
            Items = records,
            TotalRecords = await dao.Count(query)
        };
    }
    
    public async Task<int?> Insert(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity);
        return await dao.Exec(entity.Insert(record));
    }

    public async Task<int?> Update(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity);
        return await dao.Exec(entity.Update(record));
    }

    public async Task<int?> Delete(string entityName, JsonElement ele)
    {
        var entity = await schemaService.GetEntityByName(entityName);
        if (entity is null) return null;

        var record = RecordParser.Parse(ele, entity);
        return await dao.Exec(entity.Delete(record));
    }

   

    public async Task<int?> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = await FindAttribute(entityName, attributeName);
        if (attribute is null || attribute.Crosstable is null)
        {
            return null;
        }

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, attribute.Crosstable.TargetEntity));
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
            RecordParser.Parse(ele, attribute.Crosstable.TargetEntity));
        return await dao.Exec(attribute.Crosstable.Insert(strId,items.ToArray() ));
        }

    public async Task<ListResult?> CrosstableList(string entityName, string strId, string attributeName, bool exclude)
    {
        var attribute =await FindAttribute(entityName, attributeName);
        if (attribute is null || attribute.Crosstable is null)
        {
            return null;
        }
        
        var selectAttributes = attribute.Crosstable.TargetEntity.GetAttributes(null, Entity.InListOrDetail.InList, null);
        var query = attribute.Crosstable.Many(selectAttributes,exclude, attribute.Crosstable.FromAttribute.CastToDatabaseType(strId));
        return new ListResult
        {
            Items = await dao.Many(query),
            TotalRecords = await dao.Count(query)
        };
    }
    public async Task AttachCrosstable(Attribute attribute, Record[] items, Func<Entity, Attribute[]> getFields)
    {
        var ids = attribute.Parent?.PrimaryKeyAttribute().GetValues(items);
        if (ids is null || ids.Length == 0)
        {
            return;
        }

        var cross = attribute.Crosstable;
        ArgumentNullException.ThrowIfNull(cross);

        var query = cross.Many(getFields(cross.TargetEntity), ids);
        var targetRecords = await dao.Many(query);
        if (targetRecords is null)
        {
            return;
        }

        var group = targetRecords.GroupBy(x => x[cross.FromAttribute.Field], x=>x);
        foreach (var grouping in group)
        {
            var filteredItems = items.Where(local => local[attribute.Parent?.PrimaryKey ?? ""].Equals(grouping.Key));
            foreach (var item in filteredItems)
            {
                item[attribute.Field] = grouping.ToArray();
            }
        }
    }
    public async Task AttachLookup(Attribute lookupAttribute, Record[] items, Func<Entity, Attribute[]> getFields)
    {
        var lookupEntity = await schemaService.GetEntityByName(lookupAttribute.GetLookupEntityName());
        ArgumentNullException.ThrowIfNull(lookupEntity);
        var ids = lookupAttribute.GetValues(items);
        var lookupRecords = await dao.Many(lookupEntity.Many(ids, getFields(lookupEntity)));
        ArgumentNullException.ThrowIfNull(lookupRecords);
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

        return entity.FindOneAttribute(attributeName);
    }

}