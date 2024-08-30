using System.Text.Json;
using FluentCMS.Services;
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
        var query = entity.ByIdQuery(idValue, entity.Attributes.GetLocalAttributes(attributes),null);
        return NotNull(await queryKateQueryExecutor.One(query,cancellationToken)).ValOrThrow($"not find record by [{id}]");
    }
    
    public async Task<Record> One(string entityName, string id, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var meta = new EntityMeta(entityName, id);
        var hookReturn = new HookReturn();
        var filters = new Filters();
        var hookParam = new HookParameter{Filters= filters};
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeReadOne, meta, hookParam, hookReturn);
        if (exit)
        {
            return hookReturn.Record;
        }

        var idValue = schemaService.CastToDatabaseType(entity.PrimaryKeyAttribute(), id);
        var query = entity.ByIdQuery(idValue,entity.Attributes.GetLocalAttributes(InListOrDetail.InDetail),filters);
        var record = NotNull(await queryKateQueryExecutor.One(query,cancellationToken)).ValOrThrow($"not find record by [{id}]");

        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.lookup,InListOrDetail.InDetail))
        {
            attribute.Children = [attribute.Lookup!.PrimaryKeyAttribute(), attribute.Lookup.DisplayTitleAttribute()];
            await AttachLookup(attribute, [record],cancellationToken);
        }

        await hookRegistry.Trigger(provider, Occasion.AfterReadOne, meta, new HookParameter{Record = record});
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
        var exit = await hookRegistry.Trigger(provider, Occasion.BeforeReadList, meta, hookData);
        if (exit)
        {
            return null;
        }

        var attributes = entity.Attributes.GetLocalAttributes(InListOrDetail.InList);
        var query = CheckResult(entity.ListQuery(filters, sorts, pagination, null, attributes,schemaService.CastToDatabaseType));
        var records = await queryKateQueryExecutor.Many(query,cancellationToken);

        var ret = new ListResult
        {
            Items = [..records]
        };

        if (records.Length > 0)
        {
            foreach (var listLookupsAttribute in entity.Attributes.GetAttributesByType(DisplayType.lookup, InListOrDetail.InList))
            {
                listLookupsAttribute.Children = [listLookupsAttribute.Lookup!.PrimaryKeyAttribute(),listLookupsAttribute.Lookup!.DisplayTitleAttribute()];
                await AttachLookup(listLookupsAttribute, records, cancellationToken);
            }
            ret.TotalRecords = await queryKateQueryExecutor.Count(entity.CountQuery(filters),cancellationToken);
        }
        hookData = new HookParameter
        {
            ListResult = ret
        };
        await hookRegistry.Trigger(provider, Occasion.AfterReadList, meta, hookData);
        return ret;
    }

    public async Task<Record> Insert(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName,cancellationToken));
        var record = CheckResult(RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType));
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
        var record = CheckResult(RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType));
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
        var record = CheckResult(RecordParser.Parse(ele, entity, schemaService.CastToDatabaseType));
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

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");

        var items = elements.Select(ele =>
            CheckResult(RecordParser.Parse(ele, crossTable.TargetEntity, schemaService.CastToDatabaseType))).ToArray();

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

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");

        var items = elements.Select(ele =>
            CheckResult(RecordParser.Parse(ele, crossTable.TargetEntity, schemaService.CastToDatabaseType))).ToArray();
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

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");
        var selectAttributes = crossTable.TargetEntity.Attributes.GetLocalAttributes(InListOrDetail.InList);
        var query = crossTable.Many(selectAttributes, exclude,  schemaService.CastToDatabaseType(crossTable.FromAttribute,strId));
        return new ListResult
        {
            Items = [..await queryKateQueryExecutor.Many(query,cancellationToken)],
            TotalRecords = await queryKateQueryExecutor.Count(query,cancellationToken)
        };
    }

    public async Task AttachRelatedEntity(Attribute[]? attributes, Record[] items, CancellationToken cancellationToken)
    {
        if (attributes is null)
        {
            return;
        }
        
        foreach (var attribute in attributes.GetAttributesByType(DisplayType.lookup))
        {
            await AttachLookup(attribute, items, cancellationToken);
        }

        foreach (var attribute in attributes.GetAttributesByType(DisplayType.crosstable))
        {
            await AttachCrosstable(attribute, items, cancellationToken);
        }
    }

    private async Task AttachCrosstable(Attribute attribute, Record[] items, CancellationToken cancellationToken)
    {
        //no need to attach, ignore
        var ids = attribute.Parent?.PrimaryKeyAttribute().GetValues(items);
        if (ids is null || ids.Length == 0)
        {
            return;
        }

        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attribute.FullName()}");
        var fields = attribute.Children.GetLocalAttributes();
        if (fields.Length == 0)
        {
            fields = cross.TargetEntity.Attributes.GetLocalAttributes();
        }
        
        var query = cross.Many(fields, ids);
        var targetRecords = await queryKateQueryExecutor.Many(query,cancellationToken);
        await AttachRelatedEntity(attribute.Children, targetRecords, cancellationToken);
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

    private async Task AttachLookup(Attribute attribute, Record[] items, CancellationToken cancellationToken)
    {
        var lookupEntity = NotNull(attribute.Lookup)
            .ValOrThrow($"not find lookup entity from {attribute.FullName()}");

        var children = attribute.Children?.GetLocalAttributes()??[];
        if (children.Length == 0)
        {
            children = lookupEntity.Attributes.GetLocalAttributes();
        }

        if (children.FindOneAttribute(lookupEntity.PrimaryKey) == null)
        {
            children = [..children, lookupEntity.PrimaryKeyAttribute()];
        }
        
        var manyQuery = lookupEntity.ManyQuery(attribute.GetValues(items), children);
        if (manyQuery.IsFailed)
        {
            return;
        }

        var targetRecords = await queryKateQueryExecutor.Many(manyQuery.Value,cancellationToken);
        await AttachRelatedEntity(attribute.Children, targetRecords, cancellationToken);
        
        foreach (var lookupRecord in targetRecords)
        {
            var lookupId = lookupRecord[lookupEntity.PrimaryKey];
            foreach (var item in items.Where(local =>
                         local[attribute.Field] is not null && local[attribute.Field].Equals(lookupId)))
            {
                item[attribute.Field] = lookupRecord;
            }
        }
    }

    private async Task<Attribute?> FindAttribute(string entityName, string attributeName, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await schemaService.GetEntityByNameOrDefault(entityName, cancellationToken));
        return entity.Attributes.FindOneAttribute(attributeName);
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