using System.Text.Json;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.Qs;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntityService(
    IServiceProvider provider,
    KateQueryExecutor queryKateQueryExecutor,
    ISchemaService schemaService,
    HookRegistry hookRegistry) : IEntityService
{
    public async Task<Record> OneByAttributes(string entityName, string id, string[] attributes, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var idValue = schemaService.CastToDatabaseType(entity.PrimaryKeyAttribute(), id);
        var query = entity.ByIdQuery(idValue, entity.LocalAttributes(attributes),null);
        return NotNull(await queryKateQueryExecutor.One(query,cancellationToken)).ValOrThrow($"not find record by [{id}]");
    }
    
    public async Task<Record> One(string entityName, string id, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var meta = new EntityMeta(entityName, id);
        var hookReturn = new HookReturn();
        var filters = new Filters();
        var hookParam = new HookParameter{Filters= filters};
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeQueryOne, meta, hookParam, hookReturn);
        if (exit)
        {
            return hookReturn.Record;
        }

        var idValue = schemaService.CastToDatabaseType(entity.PrimaryKeyAttribute(), id);
        var query = entity.ByIdQuery(idValue,entity.LocalAttributes(InListOrDetail.InDetail),filters);
        var record = NotNull(await queryKateQueryExecutor.One(query,cancellationToken)).ValOrThrow($"not find record by [{id}]");

        foreach (var detailLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup,
                     InListOrDetail.InDetail))
        {
            await AttachLookup(detailLookupsAttribute, [record],cancellationToken,
                lookupEntity => lookupEntity.LocalAttributes(InListOrDetail.InDetail));
        }

        await hookRegistry.Trigger(provider, Occasion.AfterQueryOne, meta, new HookParameter{Record = record});
        return record;
    }

    public async Task<ListResult?> List(string entityName, Pagination? pagination, Dictionary<string, StringValues> qs, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var qsDict = new QsDict(qs);
        var filters = CheckResult(Filters.Parse(entity,qsDict,schemaService.CastToDatabaseType));
        var sorts = CheckResult(Sorts.Parse(qsDict));
        return await List(entity,filters,sorts , pagination, cancellationToken);
    }

    public async Task<ListResult?> List(string entityName, Filters? filters, Sorts? sorts, Pagination? pagination, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        return await List(entity, filters, sorts, pagination,cancellationToken);
    }
    
    private async Task<ListResult?> List(Entity entity, Filters? filters, Sorts? sorts, Pagination? pagination, CancellationToken cancellationToken)
    {
        var meta = new EntityMeta(entity.Name);
        pagination ??= new Pagination
        {
            Limit = entity.DefaultPageSize
        };

        filters ??= [];
        sorts ??= [];

        var hookData = new HookParameter
        {
            Filters = filters,
            Sorts = sorts,
            Pagination = pagination
        };
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeQueryMany, meta, hookData);
        if (exit)
        {
            return null;
        }

        var attributes = entity.LocalAttributes(InListOrDetail.InList);
        var query = CheckResult(entity.ListQuery(filters, sorts, pagination, null, attributes,schemaService.CastToDatabaseType));
        var records = await queryKateQueryExecutor.Many(query,cancellationToken);

        var ret = new ListResult
        {
            Items = [..records]
        };

        if (records.Length > 0)
        {
            foreach (var listLookupsAttribute in entity.GetAttributesByType(DisplayType.lookup, InListOrDetail.InList))
            {
                await AttachLookup(listLookupsAttribute, records, cancellationToken,lookupEntity => lookupEntity.ReferencedAttributes());
            }

            ret.TotalRecords = await queryKateQueryExecutor.Count(entity.CountQuery(filters),cancellationToken);
        }

        hookData = new HookParameter
        {
            ListResult = ret
        };
        await hookRegistry.Trigger(provider, Occasion.AfterQueryMany, meta, hookData);
        return ret;
    }

    public async Task<Record> Insert(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var record = RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType);
        return await Insert(entity, record,cancellationToken);
    }

    public async Task<Record> Insert(string entityName, Record record, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        return await Insert(entity, record, cancellationToken);
    }

    public async Task<Record> Update(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var record = RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType);
        return await Update(entity, record, cancellationToken);
    }

    public async Task<Record> Update(string entityName, Record record, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        return await Update(entity, record, cancellationToken);
    }

    public async Task<Record> Delete(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var record = RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType);
        return await Delete(entity, record,cancellationToken);
    }
    public async Task<Record> Delete(string entityName, Record record, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        return await Delete(entity, record,cancellationToken);
    }

    public async Task<int> CrosstableDelete(string entityName, string strId, string attributeName,
        JsonElement[] elements, CancellationToken cancellationToken)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName,cancellationToken))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity, schemaService.CastToDatabaseType)).ToArray();

        var meta = new EntityMeta(entityName, strId);
        var hookData = new HookParameter
        {
            Attribute = attribute,
            Records = items
        };
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeDeleteRelated, meta, hookData);
        if (exit)
        {
            return 0;
        }
        var query = crossTable.Delete( schemaService.CastToDatabaseType(crossTable.FromAttribute, meta.RecordId), items);
        var ret = await queryKateQueryExecutor.Exec(query, cancellationToken);
        hookData = new HookParameter
        {
            Attribute = attribute,
            Records = items
        };
        await hookRegistry.Trigger(provider, Occasion.AfterDeleteRelated, meta, hookData);
        return ret;
    }

    public async Task<int> CrosstableAdd(string entityName, string strId, string attributeName, JsonElement[] elements, CancellationToken cancellationToken)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName,cancellationToken))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");

        var items = elements.Select(ele =>
            RecordParser.Parse(ele, crossTable.TargetEntity, schemaService.CastToDatabaseType)).ToArray();
        var meta = new EntityMeta(entityName, strId);
        var hookData = new HookParameter
        {
            Attribute = attribute,
            Records = items,
        };
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeAddRelated, meta, hookData);
        if (exit)
        {
            return 0;
        }
        var query = crossTable.Insert( schemaService.CastToDatabaseType(crossTable.FromAttribute,strId), items);
        var ret = await queryKateQueryExecutor.Exec(query, cancellationToken);
        hookData = new HookParameter
        {
            Attribute = attribute,
            Records = items,
        };
        await hookRegistry.Trigger(provider, Occasion.AfterAddRelated, meta, hookData);
        return ret;
    }

    public async Task<ListResult> CrosstableList(string entityName, string strId, string attributeName, bool exclude, CancellationToken cancellationToken)
    {
        var attribute = NotNull(await FindAttribute(entityName, attributeName,cancellationToken))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for ${attributeName}");
        var selectAttributes = crossTable.TargetEntity.LocalAttributes(InListOrDetail.InList);
        var query = crossTable.Many(selectAttributes, exclude,  schemaService.CastToDatabaseType(crossTable.FromAttribute,strId));
        return new ListResult
        {
            Items = [..await queryKateQueryExecutor.Many(query,cancellationToken)],
            TotalRecords = await queryKateQueryExecutor.Count(query,cancellationToken)
        };
    }

    public async Task AttachCrosstable(Attribute attribute, Record[] items, CancellationToken cancellationToken,Func<Entity, Attribute[]> getFields)
    {
        //no need to attach, ignore
        var ids = attribute.Parent?.PrimaryKeyAttribute().GetValues(items);
        if (ids is null || ids.Length == 0)
        {
            return;
        }

        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable for {attribute.FullName()}");
        var query = cross.Many(getFields(cross.TargetEntity), ids);
        var targetRecords = await queryKateQueryExecutor.Many(query,cancellationToken);
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

    public async Task AttachLookup(Attribute lookupAttribute, Record[] items, CancellationToken cancellationToken,Func<Entity, Attribute[]> getFields)
    {
        var lookupEntity = NotNull(lookupAttribute.Lookup)
            .ValOrThrow($"not find lookup entity from {lookupAttribute.FullName()}");

        var manyQuery = lookupEntity.ManyQuery(lookupAttribute.GetValues(items), getFields(lookupEntity));
        if (manyQuery.IsFailed)
        {
            return;
        }

        var lookupRecords = await queryKateQueryExecutor.Many(manyQuery.Value,cancellationToken);
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

    private async Task<Attribute?> FindAttribute(string entityName, string attributeName, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName, cancellationToken));
        return entity.FindOneAttribute(attributeName);
    }

    private async Task<Record> Update(Entity entity, Record record, CancellationToken cancellationToken)
    {
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        var meta = new EntityMeta(entity.Name, id.ToString()!);
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeUpdate, meta, new HookParameter{Record = record});
        if (exit)
        {
            return record;
        }

        var query = CheckResult(entity.UpdateQuery(record)); 
        await queryKateQueryExecutor.Exec(query,cancellationToken);
        await hookRegistry.Trigger(provider, Occasion.AfterUpdate, meta, new HookParameter{Record = record});
        return record;
    }

    private async Task<Record> Insert(Entity entity, Record record,CancellationToken cancellationToken)
    {
        var meta = new EntityMeta(entity.Name);
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeInsert, meta, new HookParameter{Record = record});
        if (exit)
        {
            return record;
        }

        var query = entity.Insert(record);
        var id = await queryKateQueryExecutor.Exec(query, cancellationToken);
        record[entity.PrimaryKey] = id;
        meta = new EntityMeta(entity.Name, id.ToString());
        await hookRegistry.Trigger(provider, Occasion.AfterInsert, meta, new HookParameter{Record = record});
        return record;
    }

    private async Task<Record> Delete(Entity entity, Record record, CancellationToken cancellationToken)
    {
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        var meta = new EntityMeta(entity.Name, id.ToString()!);
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeDelete, meta, new HookParameter{Record = record});
        if (exit)
        {
            return record;
        }

        var query = CheckResult(entity.DeleteQuery(record));
        await queryKateQueryExecutor.Exec(query, cancellationToken);

        await hookRegistry.Trigger(provider, Occasion.AfterDelete, meta, new HookParameter{Record = record});
        return record;
    }
}