using System.Collections.Immutable;
using System.Text.Json;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.HookFactory;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntityService(
    IDefinitionExecutor definitionExecutor,
    IServiceProvider provider,
    KateQueryExecutor queryKateQueryExecutor,
    IEntitySchemaService entitySchemaService,
    HookRegistry hookRegistry) : IEntityService
{
    public async Task<Record> OneByAttributes(string entityName, string id, string[] attributes,
        CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var idValue = definitionExecutor.Cast(id,entity.PrimaryKeyAttribute.DataType);
        var query = entity.ByIdQuery(idValue, entity.Attributes.GetLocalAttributes(attributes), []);
        return NotNull(await queryKateQueryExecutor.One(query, cancellationToken))
            .ValOrThrow($"not find record by [{id}]");
    }

    public async Task<Record> One(string entityName, string id, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var res = await hookRegistry.EntityPreGetOne.Trigger(provider,
            new EntityPreGetOneArgs(entityName, id, default));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var idValue = definitionExecutor.Cast(id,entity.PrimaryKeyAttribute.DataType);
        var query = entity.ByIdQuery(idValue, entity.Attributes.GetLocalAttributes(InListOrDetail.InDetail), []);
        var record = NotNull(await queryKateQueryExecutor.One(query, cancellationToken))
            .ValOrThrow($"not find record by [{id}]");

        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.Lookup, InListOrDetail.InDetail))
        {
            await AttachLookup( attribute, [record], cancellationToken);
        }

        await hookRegistry.EntityPostGetOne.Trigger(provider, new EntityPostGetOneArgs(entityName, id, record));
        return record;
    }

    public async Task<ListResult?> List(string entityName, Pagination pagination, Dictionary<string, StringValues> qs,
        CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName,  cancellationToken));
        var groupQs = qs.GroupByFirstIdentifier();
        var filters = CheckResult(await FilterHelper.Parse(entity, groupQs, entitySchemaService.ResolveAttributeVector));
        var sorts = CheckResult(await SortHelper.Parse(entity, groupQs, entitySchemaService.ResolveAttributeVector));
        return await List(entity, filters, sorts, pagination, cancellationToken);
    }

    private async Task<ListResult?> List(LoadedEntity entity, ImmutableArray<ValidFilter> filters, ImmutableArray<ValidSort> sorts, Pagination pagination,
        CancellationToken cancellationToken)
    {

        var res = await hookRegistry.EntityPreGetList.Trigger(provider,
            new EntityPreGetListArgs(entity.Name, entity, filters, sorts, pagination.ToValid(entity.DefaultPageSize)));
        var attributes = entity.Attributes.GetLocalAttributes(InListOrDetail.InList);
        
        var query = CheckResult(entity.ListQuery(res.RefFilters, res.RefSorts, res.RefPagination, null, attributes));
        
        var records = await queryKateQueryExecutor.Many(query, cancellationToken);

        var ret = new ListResult(records, records.Length);
        if (records.Length > 0)
        {
            foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.Lookup, InListOrDetail.InList))
            {
                await AttachLookup(attribute, records, cancellationToken);
            }
            ret = ret with{TotalRecords = await queryKateQueryExecutor.Count(entity.CountQuery(filters), cancellationToken)};
        }

        var postRes = await hookRegistry.EntityPostGetList.Trigger(provider, new EntityPostGetListArgs(entity.Name, ret));
        return postRes.RefListResult;
    }



    public async Task<Record> Insert(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName,  cancellationToken));
        var record = CheckResult(entity.Parse(ele));
        return await Insert(entity, record, cancellationToken);
    }

    public async Task<Record> Insert(string entityName, Record record, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName,  cancellationToken));
        return await Insert(entity, record, cancellationToken);
    }

    public async Task<Record> Update(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var record = CheckResult(entity.Parse(ele));
        return await Update(entity, record, cancellationToken);
    }

    public async Task<Record> Delete(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var record = CheckResult(entity.Parse(ele));
        return await Delete(entity, record, cancellationToken);
    }

    public async Task<int> CrosstableDelete(string entityName, string strId, string attributeName,
        JsonElement[] elements, CancellationToken cancellationToken)
    {
        var attribute = NotNull(await entitySchemaService.FindAttribute(entityName, attributeName, cancellationToken))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");

        var items = elements.Select(ele =>
            CheckResult(crossTable.TargetEntity.Parse(ele))).ToArray();

        var res = await hookRegistry.CrosstablePreDel.Trigger(provider,
            new CrosstablePreDelArgs(entityName, strId, attribute, items));

        var query = crossTable.Delete(definitionExecutor.Cast(strId,crossTable.SourceAttribute.Field), res.RefItems);
        var ret = await queryKateQueryExecutor.Exec(query, cancellationToken);
        await hookRegistry.CrosstablePostDel.Trigger(provider,
            new CrosstablePostDelArgs(entityName, strId, attribute, items));
        return ret;
    }

    public async Task<int> CrosstableAdd(string entityName, string strId, string attributeName, JsonElement[] elements,
        CancellationToken cancellationToken)
    {
        var attribute = NotNull(await entitySchemaService.FindAttribute(entityName, attributeName, cancellationToken))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");

        var items = elements
            .Select(ele => CheckResult(crossTable.TargetEntity.Parse(ele))).ToArray();
        var res = await hookRegistry.CrosstablePreAdd.Trigger(provider,
            new CrosstablePreAddArgs(entityName, strId, attribute, items));

        var query = crossTable.Insert(definitionExecutor.Cast(strId,crossTable.SourceAttribute.DataType), res.RefItems);
        
        var ret = await queryKateQueryExecutor.Exec(query, cancellationToken);
        await hookRegistry.CrosstablePostAdd.Trigger(provider,
            new CrosstablePostAddArgs(entityName, strId, attribute, items));
        return ret;
    }

    public async Task<ListResult> CrosstableList(string entityName, string strId, string attributeName, bool exclude,
        Dictionary<string,StringValues> qs, Pagination pagination, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var attribute = NotNull(entity.Attributes.FindOneAttribute(attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");
        var target = crossTable.TargetEntity;
        
        var selectAttributes = target.Attributes.GetLocalAttributes(InListOrDetail.InList);
        var id = crossTable.SourceAttribute.Cast(strId);
        
        var dictionary = qs.GroupByFirstIdentifier();
        var filter = CheckResult(await FilterHelper.Parse(target, dictionary, entitySchemaService.ResolveAttributeVector));
        var sorts =CheckResult(await SortHelper.Parse(target, dictionary, entitySchemaService.ResolveAttributeVector));
        var validPagination = pagination.ToValid(crossTable.TargetEntity.DefaultPageSize);

        var pagedListQuery = exclude
            ? crossTable.GetNotRelatedItems(selectAttributes, filter, sorts, validPagination, [id])
            : crossTable.GetRelatedItems(selectAttributes, filter, sorts, validPagination, [id]);

        var countQuery = exclude
            ? crossTable.GetNotRelatedItemsCount(filter, [id])
            : crossTable.GetRelatedItemsCount(filter, [id]);
        
        return new ListResult(await queryKateQueryExecutor.Many(pagedListQuery, cancellationToken),
            await queryKateQueryExecutor.Count(countQuery, cancellationToken));
    }

    public async Task AttachRelatedEntity(LoadedEntity entity,IEnumerable<GraphAttribute>? attributes, Record[] items, CancellationToken cancellationToken)
    {
        if (attributes is null)
        {
            return;
        }

        var arr = attributes.ToArray();

        foreach (var attribute in arr.GetAttributesByType(DisplayType.Lookup))
        {
            await AttachLookup(attribute, items, cancellationToken);
        }

        foreach (var attribute in arr.GetAttributesByType(DisplayType.Crosstable))
        {
            await AttachCrosstable(entity,attribute, items, cancellationToken);
        }
    }

    private async Task AttachCrosstable(LoadedEntity sourceEntity, GraphAttribute attribute, Record[] items, CancellationToken cancellationToken)
    {
        //no need to attach, ignore
        var ids = sourceEntity.PrimaryKeyAttribute.GetUniqValues(items);
        if (ids.Length == 0)
        {
            return;
        }

        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attribute.GetFullName()}");
        var fields = attribute.Selection.GetLocalAttributes();
        var query = cross.GetRelatedItems(fields, attribute.Filters, attribute.Sorts, 
            new Pagination().ToValid(cross.TargetEntity.DefaultPageSize),
            ids);
        var targetRecords = await queryKateQueryExecutor.Many(query, cancellationToken);
        
        await AttachRelatedEntity(cross.TargetEntity,attribute.Selection, targetRecords, cancellationToken);
        
        var group = targetRecords.GroupBy(x => x[cross.SourceAttribute.Field], x => x);
        foreach (var grouping in group)
        {
            var filteredItems = items.Where(local => local[sourceEntity.PrimaryKey].Equals(grouping.Key));
            foreach (var item in filteredItems)
            {
                item[attribute.Field] = grouping.ToArray();
            }
        }
    }
    private async Task AttachLookup(LoadedAttribute attribute, Record[] records, CancellationToken cancellationToken)
    {
        var lookup = attribute.Lookup!;
        await AttachLookup(
            attribute.ToGraph() with
            {
                Selection = [lookup.PrimaryKeyAttribute.ToGraph(), lookup.LoadedTitleAttribute.ToGraph()]
            }, records, cancellationToken);
    }
    
    private async Task AttachLookup(GraphAttribute attribute, Record[] items, CancellationToken cancellationToken)
    {
        var lookupEntity = NotNull(attribute.Lookup)
            .ValOrThrow($"not find lookup entity from {attribute.GetFullName()}");

        var children = attribute.Selection.GetLocalAttributes(); 
        if (children.FindOneAttribute(lookupEntity.PrimaryKey) == null)
        {
            children = [..children, lookupEntity.PrimaryKeyAttribute.ToGraph()];
        }

        var ids = attribute.GetUniqValues(items);
        if (ids.Length == 0)
        {
            return;
        }
        
        var query = lookupEntity.ManyQuery(ids, children);
        var targetRecords = await queryKateQueryExecutor.Many(query, cancellationToken);
        await AttachRelatedEntity(lookupEntity,attribute.Selection, targetRecords, cancellationToken);

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

    private async Task<Record> Update(LoadedEntity entity, Record record, CancellationToken cancellationToken)
    {
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        CheckResult(entity.ValidateLocalAttributes(record));
        CheckResult(entity.ValidateTitleAttributes(record));


        var res = await hookRegistry.EntityPreUpdate.Trigger(provider,
            new EntityPreUpdateArgs(entity.Name, id.ToString()!, record));

        var query = CheckResult(entity.UpdateQuery(res.RefRecord));
        await queryKateQueryExecutor.Exec(query, cancellationToken);
        await hookRegistry.EntityPostUpdate.Trigger(provider,new EntityPostUpdateArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    private async Task<Record> Insert(LoadedEntity entity, Record record, CancellationToken cancellationToken)
    {
        CheckResult(entity.ValidateLocalAttributes(record));
        CheckResult(entity.ValidateTitleAttributes(record));

        var res = await hookRegistry.EntityPreAdd.Trigger(provider,
            new EntityPreAddArgs(entity.Name, record));
        record = res.RefRecord;
        
        var query = entity.Insert(record);
        var id = await queryKateQueryExecutor.Exec(query, cancellationToken);
        record[entity.PrimaryKey] = id;
        
        await hookRegistry.EntityPostAdd.Trigger(provider,
                new EntityPostAddArgs(entity.Name, id.ToString(), record));
        return record;
    }

    private async Task<Record> Delete(LoadedEntity entity, Record record, CancellationToken cancellationToken)
    {
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        var res = await hookRegistry.EntityPreDel.Trigger(provider,
                new EntityPreDelArgs(entity.Name, id.ToString()!, record));
        record = res.RefRecord;


        var query = CheckResult(entity.DeleteQuery(record));
        await queryKateQueryExecutor.Exec(query, cancellationToken);
        (_, _, record) = await hookRegistry.EntityPostDel.Trigger(provider,
            new EntityPostDelArgs(entity.Name, id.ToString()!, record));
        return record;
    }
}