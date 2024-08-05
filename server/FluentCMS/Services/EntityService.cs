using System.Text.Json;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.Qs;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;
using static InvalidParamExceptionFactory;

public sealed class EntityService(
    IDefinitionExecutor definitionExecutor,
    IServiceProvider provider,
    KateQueryExecutor queryKateQueryExecutor,
    ISchemaService schemaService,
    HookFactory hookFactory) : IEntityService
{
    public async Task<object> One(string entityName, string id)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var (ret, next) = await hookFactory.ExecuteStringToObject(provider, Occasion.BeforeQueryOne, entityName, id);
        if (next == Next.Exit)
        {
            return ret;
        }

        id = (string)ret;
        var query = entity.ByIdQuery(CastToDatabaseType(entity.PrimaryKeyAttribute(),id), entity.LocalAttributes(InListOrDetail.InDetail));
        var record = NotNull(await queryKateQueryExecutor.One(query))
            .ValOrThrow($"not find record by [{id}]");

        foreach (var detailLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup,
                     InListOrDetail.InDetail))
        {
            await AttachLookup(detailLookupsAttribute, [record],
                lookupEntity => lookupEntity.LocalAttributes(InListOrDetail.InDetail));
        }

        var (res, _) = await hookFactory.ExecuteRecordToObject(provider, Occasion.AfterQueryOne, entityName, record);
        return res;
    }

    public async Task<object> List(string entityName, Pagination? pagination, Dictionary<string, StringValues> qs)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        
        var qsDict = new QsDict(qs);
        var filters = CheckResult(Filters.Parse(entity,qsDict,CastToDatabaseType));
        var sorts = CheckResult(Sorts.Parse(qsDict));
        return await List(entity,filters,sorts , pagination);
    }

    public async Task<object> List(string entityName, Filters? filters, Sorts? sorts, Pagination? pagination)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        return await List(entity, filters, sorts, pagination);
    }
    
    private async Task<object> List(Entity entity, Filters? filters, Sorts? sorts, Pagination? pagination)
    {
        pagination ??= new Pagination
        {
            Limit = entity.DefaultPageSize
        };

        filters ??= [];
        sorts ??= [];

        var (res, next) = await hookFactory.ExecuteBeforeQuery(provider, Occasion.BeforeQueryMany, entity.Name, 
            filters,
            sorts,
            pagination);
        if (next == Next.Exit)
        {
            return NotNull(res)
                .ValOrThrow($"Before Query Many hook of {entity.Name}, required exit, but the hook return no value");
        }

        var attributes = entity.LocalAttributes(InListOrDetail.InList);
        var query = entity.ListQuery(filters, sorts, pagination, null, attributes,CastToDatabaseType);
        var records = await queryKateQueryExecutor.Many(query);

        var ret = new ListResult
        {
            Items = [..records]
        };

        if (records.Length > 0)
        {
            foreach (var listLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup, InListOrDetail.InList))
            {
                await AttachLookup(listLookupsAttribute, records, lookupEntity => lookupEntity.ReferencedAttributes());
            }

            ret.TotalRecords = await queryKateQueryExecutor.Count(entity.CountQuery(filters));
        }

        await hookFactory.ExecuteAfterQuery(provider, Occasion.AfterQueryMany, entity.Name, ret);
        return ret;
    }

    public async Task<object> Insert(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity, CastToDatabaseType);
        return await Insert(entity, record);
    }

    public async Task<object> Insert(string entityName, Record record)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        return await Insert(entity, record);
    }

    public async Task<object> Update(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity,CastToDatabaseType);
        return await Update(entity, record);
    }

    public async Task<object> Update(string entityName, Record record)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        return await Update(entity, record);
    }

    public async Task<object> Delete(string entityName, JsonElement ele)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        var record = RecordParser.Parse(ele, entity,CastToDatabaseType);
        return await Delete(entity, record);
    }
    public async Task<object> Delete(string entityName, Record record)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
        return await Delete(entity, record);
    }

    public async Task<int> CrosstableDelete(string entityName, string strId, string attributeName,
        JsonElement[] elements)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity,CastToDatabaseType));
        return await queryKateQueryExecutor.Exec(crossTable.Delete(CastToDatabaseType(crossTable.FromAttribute,strId), items.ToArray()));
    }

    public async Task<int> CrosstableSave(string entityName, string strId, string attributeName, JsonElement[] elements)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity,CastToDatabaseType));
        return await queryKateQueryExecutor.Exec(crossTable.Insert(CastToDatabaseType(crossTable.FromAttribute,strId), items.ToArray()));
    }

    public async Task<ListResult> CrosstableList(string entityName, string strId, string attributeName, bool exclude)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");
        var selectAttributes = crossTable.TargetEntity.LocalAttributes(InListOrDetail.InList);
        var query = crossTable.Many(selectAttributes, exclude, CastToDatabaseType(crossTable.FromAttribute,strId));
        return new ListResult
        {
            Items = [..await queryKateQueryExecutor.Many(query)],
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
        var group = targetRecords.GroupBy(x => x[cross.FromAttribute.Field], x => x);
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

    private async Task<object> Update(Entity entity, Record record)
    {
        var (res, next) = await hookFactory.ExecuteRecordToRecord(provider, Occasion.BeforeUpdate, entity.Name, record);
        if (next == Next.Exit)
        {
            return res;
        }

        record = res;
        await queryKateQueryExecutor.Exec(CheckResult(entity.UpdateQuery(record)));
        var (hookRes, _) = await hookFactory.ExecuteRecordToObject(provider, Occasion.AfterUpdate, entity.Name, record);
        return hookRes;
    }

    private async Task<object> Insert(Entity entity, Record record)
    {
        var (res, next) = await hookFactory.ExecuteRecordToRecord(provider, Occasion.BeforeInsert, entity.Name, record);
        if (next == Next.Exit)
        {
            return res;
        }

        record = res;
        var id = await queryKateQueryExecutor.Exec(entity.Insert(record));
        record[entity.PrimaryKey] = id;
        var (hookRes, _) = await hookFactory.ExecuteRecordToObject(provider, Occasion.AfterInsert, entity.Name, record);
        return hookRes;
    }

    private async Task<object> Delete(Entity entity, Record record)
    {
        var (res, next) = await hookFactory.ExecuteRecordToRecord(provider, Occasion.BeforeDelete, entity.Name, record);
        if (next == Next.Exit)
        {
            return res;
        }

        record = res;
        await queryKateQueryExecutor.Exec(CheckResult(entity.DeleteQuery(record)));

        var (ret, _) = await hookFactory.ExecuteRecordToObject(provider, Occasion.AfterDelete, entity.Name, record);
        return ret;
    }
    
    private object CastToDatabaseType(Attribute attribute, string str)
    {
        return definitionExecutor.CastToDatabaseType(attribute.DataType, str);
    }
}