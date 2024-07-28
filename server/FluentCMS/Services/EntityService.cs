using System.Text.Json;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;
using static InvalidParamExceptionFactory;

public class EntityService(KateQueryExecutor queryKateQueryExecutor, ISchemaService schemaService) : IEntityService
{
    public async Task<Record?> One(string entityName, string id)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record =
            await queryKateQueryExecutor.One(entity.ByIdQuery(id,
                entity.LocalAttributes(InListOrDetail.InDetail)));
        if (record is null)
        {
            return record;
        }

        foreach (var detailLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup,
                     InListOrDetail.InDetail))
        {
            await AttachLookup(detailLookupsAttribute, [record],
                lookupEntity => lookupEntity.LocalAttributes(InListOrDetail.InDetail));
        }

        return record;
    }

    public async Task<ListResult> List(string entityName, Pagination? pagination, Dictionary<string,StringValues> qs)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        pagination ??= new Pagination
        {
            Limit = entity.DefaultPageSize
        };

        var filters = new Filters(qs);
        var query = entity.ListQuery(filters, CheckResult(Sorts.Parse(qs)), pagination, null,
            entity.LocalAttributes(InListOrDetail.InList));


        var records = await queryKateQueryExecutor.Many(query);
        if (records.Length == 0)
        {
            return new ListResult
            {
                Items = [],
                TotalRecords = 0,
            };
        }

        foreach (var listLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup, InListOrDetail.InList))
        {
            await AttachLookup(listLookupsAttribute, records, lookupEntity => lookupEntity.ReferencedAttributes());
        }

        return new ListResult
        {
            Items = records,
            TotalRecords = await queryKateQueryExecutor.Count(entity.CountQuery(filters))
        };
    }
    
    public async Task<int> Insert(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity);
        return await queryKateQueryExecutor.Exec(entity.Insert(record));
    }

    public async Task<int> Update(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity);
        return await queryKateQueryExecutor.Exec(CheckResult(entity.UpdateQuery(record)));
    }

    public async Task<int> Delete(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity);
        return await queryKateQueryExecutor.Exec(CheckResult(entity.DeleteQuery(record)));
    }

    public async Task<int> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");
        
        var crossTable = NotNull(attribute.Crosstable).
            ValOrThrow($"not find crosstable for ${attributeName}");
        
        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity));
        return await queryKateQueryExecutor.Exec(crossTable.Delete(strId, items.ToArray()));
    }

    public async Task<int> CrosstableSave(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity));
        return await queryKateQueryExecutor.Exec(crossTable.Insert(strId, items.ToArray()));
    }

    public async Task<ListResult> CrosstableList(string entityName, string strId, string attributeName, bool exclude)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");
        var selectAttributes = crossTable.TargetEntity.LocalAttributes(InListOrDetail.InList);
        var query = crossTable.Many(selectAttributes, exclude, crossTable.FromAttribute.CastToDatabaseType(strId));
        return new ListResult
        {
            Items = await queryKateQueryExecutor.Many(query),
            TotalRecords = await queryKateQueryExecutor.Count(query)
        };
    }

    public async Task AttachCrosstable(Attribute attribute, Record[] items, Func<Entity, Attribute[]> getFields)
    {
        //no need to attach, ignore
        var ids = attribute.Parent?.PrimaryKeyAttribute().GetValues(items);
        if (ids is null || ids.Length == 0)
        {
            return;
        }

        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for {attribute.FullName()}");
        var query = cross.Many(getFields(cross.TargetEntity), ids);
        var targetRecords = await queryKateQueryExecutor.Many(query);
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
        var lookupEntity = NotNull(lookupAttribute.Lookup)
            .ValOrThrow($"not find lookup entity from {lookupAttribute.FullName()}");
        
        var manyQuery = lookupEntity.ManyQuery(lookupAttribute.GetValues(items), getFields(lookupEntity));
        if (manyQuery.IsFailed)
        {
            return;
        }
        
        var lookupRecords = await queryKateQueryExecutor.Many(manyQuery.Value);
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
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        return entity.FindOneAttribute(attributeName);
    }
}