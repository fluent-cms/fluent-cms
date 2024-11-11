using System.Collections.Immutable;
using System.Text.Json;
using FluentCMS.Services;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.HookFactory;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntityService(
    IServiceProvider provider,
    KateQueryExecutor kateQueryExecutor,
    IEntitySchemaService resolver,
    HookRegistry hookRegistry
) : IEntityService
{
    public async Task<Record> OneByAttributes(string entityName, string id, string[] attributes,
        CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        if (!resolver.ResolveVal(entity.PrimaryKeyAttribute, id, out var idValue))
        {
            throw new InvalidParamException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        var query = entity.ByIdQuery(idValue!, entity.Attributes.GetLocalAttrs(attributes), []);
        return NotNull(await kateQueryExecutor.One(query, cancellationToken))
            .ValOrThrow($"not find record by [{id}]");
    }

    public async Task<Record> One(string entityName, string id, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        var res = await hookRegistry.EntityPreGetOne.Trigger(provider,
            new EntityPreGetOneArgs(entityName, id, default));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        if (!resolver.ResolveVal(entity.PrimaryKeyAttribute, id, out var idValue))
        {
            throw new InvalidParamException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        var query = entity.ByIdQuery(idValue!, entity.Attributes.GetLocalAttrs(InListOrDetail.InDetail), []);
        var record = NotNull(await kateQueryExecutor.One(query, cancellationToken))
            .ValOrThrow($"not find record by [{id}]");

        foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup, InListOrDetail.InDetail))
        {
            await LoadLookupData(attribute, [record], cancellationToken);
        }

        await hookRegistry.EntityPostGetOne.Trigger(provider, new EntityPostGetOneArgs(entityName, id, record));
        return record;
    }

    public async Task<ListResult?> List(string entityName, Pagination pagination, Dictionary<string, StringValues> qs,
        CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        var groupQs = qs.GroupByFirstIdentifier();

        var filters = CheckResult(await FilterHelper.Parse(entity, groupQs, resolver,resolver));
        var sorts = CheckResult(await SortHelper.Parse(entity, groupQs, resolver));
        return await List(entity, filters, sorts, pagination, cancellationToken);
    }

    private async Task<ListResult?> List(LoadedEntity entity, ImmutableArray<ValidFilter> filters,
        ImmutableArray<ValidSort> sorts, Pagination pagination,
        CancellationToken cancellationToken)
    {

        var res = await hookRegistry.EntityPreGetList.Trigger(provider,
            new EntityPreGetListArgs(entity.Name, entity, filters, sorts, pagination.ToValid(entity.DefaultPageSize)));
        var attributes = entity.Attributes.GetLocalAttrs(InListOrDetail.InList);

        var query = entity.ListQuery(res.RefFilters, res.RefSorts, res.RefPagination, null, attributes);

        var records = await kateQueryExecutor.Many(query, cancellationToken);

        var ret = new ListResult(records, records.Length);
        if (records.Length > 0)
        {
            foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup, InListOrDetail.InList))
            {
                await LoadLookupData(attribute, records, cancellationToken);
            }

            ret = ret with
            {
                TotalRecords = await kateQueryExecutor.Count(entity.CountQuery(filters), cancellationToken)
            };
        }

        var postRes =
            await hookRegistry.EntityPostGetList.Trigger(provider, new EntityPostGetListArgs(entity.Name, ret));
        return postRes.RefListResult;
    }



    public async Task<Record> Insert(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        var record = CheckResult(entity.Parse(ele, resolver));
        return await Insert(entity, record, cancellationToken);
    }

    public async Task<Record> Insert(string entityName, Record record, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        return await Insert(entity, record, cancellationToken);
    }

    public async Task<Record> Update(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        var record = CheckResult(entity.Parse(ele, resolver));
        return await Update(entity, record, cancellationToken);
    }

    public async Task<Record> Delete(string entityName, JsonElement ele, CancellationToken cancellationToken)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, cancellationToken));
        var record = CheckResult(entity.Parse(ele, resolver));
        return await Delete(entity, record, cancellationToken);
    }

    public async Task<int> CrosstableDelete(string entityName, string strId, string attributeName,
        JsonElement[] elements, CancellationToken cancellationToken)
    {
        var ctx = await ResolveCrosstableCtx(entityName, strId, attributeName, cancellationToken);
        var items = elements.Select(ele =>
            CheckResult(ctx.Crosstable.TargetEntity.Parse(ele, resolver))).ToArray();

        var res = await hookRegistry.CrosstablePreDel.Trigger(provider,
            new CrosstablePreDelArgs(entityName, strId, ctx.Attribute, items));

        var query = ctx.Crosstable.Delete(ctx.Id, res.RefItems);
        var ret = await kateQueryExecutor.Exec(query, cancellationToken);
        await hookRegistry.CrosstablePostDel.Trigger(provider,
            new CrosstablePostDelArgs(entityName, strId, ctx.Attribute, items));
        return ret;
    }

    public async Task<int> CrosstableAdd(string entityName, string strId, string attributeName, JsonElement[] elements,
        CancellationToken token)
    {
        var ctx = await ResolveCrosstableCtx(entityName, strId, attributeName, token);

        var items = elements
            .Select(ele => CheckResult(ctx.Crosstable.TargetEntity.Parse(ele, resolver))).ToArray();
        var res = await hookRegistry.CrosstablePreAdd.Trigger(provider,
            new CrosstablePreAddArgs(entityName, strId, ctx.Attribute, items));
        var query = ctx.Crosstable.Insert(ctx.Id, res.RefItems);

        var ret = await kateQueryExecutor.Exec(query, token);
        await hookRegistry.CrosstablePostAdd.Trigger(provider,
            new CrosstablePostAddArgs(entityName, strId, ctx.Attribute, items));
        return ret;
    }


    public async Task<ListResult> CrosstableList(string entityName, string strId, string attributeName, bool exclude,
        Dictionary<string, StringValues> qs, Pagination pagination, CancellationToken cancellationToken)
    {
        var ctx = await ResolveCrosstableCtx(entityName, strId, attributeName, cancellationToken);
        var target = ctx.Crosstable.TargetEntity;

        var selectAttributes = target.Attributes.GetLocalAttrs(InListOrDetail.InList);

        var dictionary = qs.GroupByFirstIdentifier();
        var filter = CheckResult(await FilterHelper.Parse(target, dictionary, resolver, resolver));
        var sorts = CheckResult(await SortHelper.Parse(target, dictionary, resolver));
        var validPagination = pagination.ToValid(ctx.Crosstable.TargetEntity.DefaultPageSize);

        var pagedListQuery = exclude
            ? ctx.Crosstable.GetNotRelatedItems(selectAttributes, filter, sorts, validPagination, [ctx.Id])
            : ctx.Crosstable.GetRelatedItems(selectAttributes, filter, sorts, null, validPagination, [ctx.Id]);

        var countQuery = exclude
            ? ctx.Crosstable.GetNotRelatedItemsCount(filter, [ctx.Id])
            : ctx.Crosstable.GetRelatedItemsCount(filter, [ctx.Id]);

        return new ListResult(await kateQueryExecutor.Many(pagedListQuery, cancellationToken),
            await kateQueryExecutor.Count(countQuery, cancellationToken));
    }

    private async Task LoadLookupData(LoadedAttribute attribute, Record[] items, CancellationToken cancellationToken)
    {
        var ids = attribute.GetUniq(items);
        if (ids.Length == 0)
        {
            return;
        }

        var lookupEntity = NotNull(attribute.Lookup)
            .ValOrThrow($"not find lookup entity from {attribute.AddTableModifier()}");

        var query = lookupEntity.ManyQuery(ids,
            [attribute.Lookup!.PrimaryKeyAttribute, attribute.Lookup!.LoadedTitleAttribute]);
        var targetRecords = await kateQueryExecutor.Many(query, cancellationToken);
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
        await kateQueryExecutor.Exec(query, cancellationToken);
        await hookRegistry.EntityPostUpdate.Trigger(provider,
            new EntityPostUpdateArgs(entity.Name, id.ToString()!, record));
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
        var id = await kateQueryExecutor.Exec(query, cancellationToken);
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
        await kateQueryExecutor.Exec(query, cancellationToken);
        (_, _, record) = await hookRegistry.EntityPostDel.Trigger(provider,
            new EntityPostDelArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    record CrosstableContext(LoadedEntity Entity, LoadedAttribute Attribute, Crosstable Crosstable, Object Id);

    private async Task<CrosstableContext> ResolveCrosstableCtx(string entityName, string strId, string attributeName,
        CancellationToken token)
    {
        var entity = CheckResult(await resolver.GetLoadedEntity(entityName, token));
        var attribute = NotNull(entity.Attributes.FindOneAttr(attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");
        if (!resolver.ResolveVal(crossTable.SourceAttribute, strId, out var id))
        {
            throw new InvalidParamException($"Failed to cast {strId} to {crossTable.SourceAttribute.DataType}");
        }

        return new CrosstableContext(entity, attribute, crossTable, id!);

    }
}