using System.Collections.Immutable;
using System.Text.Json;
using FluentCMS.Services;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class EntityService(
    IServiceProvider provider,
    KateQueryExecutor queryExecutor,
    IEntitySchemaService entitySchemaSvc,
    HookRegistry hookRegistry
) : IEntityService
{
    public async Task<Record> OneByAttributes(string entityName, string id, string[] attributes,
        CancellationToken token)
    {
        var ctx = await ResolveOneContext(entityName, id, token);
        var query = ctx.Entity.ByIdQuery(ctx.Id, ctx.Entity.Attributes.GetLocalAttrs(attributes), []);
        return NotNull(await queryExecutor.One(query, token))
            .ValOrThrow($"not find record by [{id}]");
    }

    public async Task<Record> One(string entityName, string id, CancellationToken token)
    {
        var ctx = await ResolveOneContext(entityName, id, token);
        var res = await hookRegistry.EntityPreGetOne.Trigger(provider,
            new EntityPreGetOneArgs(entityName, id, default));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var query = ctx.Entity.ByIdQuery(ctx.Id, ctx.Entity.Attributes.GetLocalAttrs(InListOrDetail.InDetail), []);
        var record = NotNull(await queryExecutor.One(query, token))
            .ValOrThrow($"not find record by [{id}]");

        foreach (var attribute in ctx.Entity.Attributes.GetAttrByType(DisplayType.Lookup, InListOrDetail.InDetail))
        {
            await LoadLookupData(attribute, [record], token);
        }

        await hookRegistry.EntityPostGetOne.Trigger(provider, new EntityPostGetOneArgs(entityName, id, record));
        return record;
    }

    public async Task<ListResult?> List(string name, Pagination pagination, StrArgs args, CancellationToken token)
    {
        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(name, token));
        var groupQs = args.GroupByFirstIdentifier();

        var filters = Ok(await FilterHelper.Parse(entity, groupQs, entitySchemaSvc, entitySchemaSvc));
        var sorts = Ok(await SortHelper.Parse(entity, groupQs, entitySchemaSvc));
        return await List(entity, [..filters], [..sorts], pagination, token);
    }
    public async Task<Record> Insert(string name, JsonElement ele, CancellationToken token)
    {
        return await Insert(await ResolveRecordContext(name, ele, token), token);
    }

    public async Task<Record> Update(string name, JsonElement ele, CancellationToken token)
    {
        return await Update(await ResolveRecordContext(name, ele, token), token);
    }

    public async Task<Record> Delete(string name, JsonElement ele, CancellationToken token)
    {
        return await Delete(await ResolveRecordContext(name, ele, token), token);
    }

    public async Task<int> CrosstableDelete(string name, string id, string attr, JsonElement[] elements,
        CancellationToken token)
    {
        var ctx = await ResolveCrosstableCtx(name, id, attr, token);
        var items = elements.Select(ele =>
            Ok(ctx.Crosstable.TargetEntity.Parse(ele, entitySchemaSvc))).ToArray();

        var res = await hookRegistry.CrosstablePreDel.Trigger(provider,
            new CrosstablePreDelArgs(name, id, ctx.Attribute, items));

        var query = ctx.Crosstable.Delete(ctx.Id, res.RefItems);
        var ret = await queryExecutor.Exec(query, token);
        await hookRegistry.CrosstablePostDel.Trigger(provider,
            new CrosstablePostDelArgs(name, id, ctx.Attribute, items));
        return ret;
    }

    public async Task<int> CrosstableAdd(string name, string id, string attr, JsonElement[] elements,
        CancellationToken token)
    {
        var ctx = await ResolveCrosstableCtx(name, id, attr, token);

        var items = elements
            .Select(ele => Ok(ctx.Crosstable.TargetEntity.Parse(ele, entitySchemaSvc))).ToArray();
        var res = await hookRegistry.CrosstablePreAdd.Trigger(provider,
            new CrosstablePreAddArgs(name, id, ctx.Attribute, items));
        var query = ctx.Crosstable.Insert(ctx.Id, res.RefItems);

        var ret = await queryExecutor.Exec(query, token);
        await hookRegistry.CrosstablePostAdd.Trigger(provider,
            new CrosstablePostAddArgs(name, id, ctx.Attribute, items));
        return ret;
    }


    public async Task<ListResult> CrosstableList(string name, string id, string attr, bool exclude,
        StrArgs args, Pagination pagination, CancellationToken token)
    {
        var ctx = await ResolveCrosstableCtx(name, id, attr, token);
        var target = ctx.Crosstable.TargetEntity;

        var selectAttributes = target.Attributes.GetLocalAttrs(InListOrDetail.InList);

        var dictionary = args.GroupByFirstIdentifier();
        var filter = Ok(await FilterHelper.Parse(target, dictionary, entitySchemaSvc, entitySchemaSvc));
        var sorts = Ok(await SortHelper.Parse(target, dictionary, entitySchemaSvc));
        var validPagination = PaginationHelper.ToValid(pagination,target.DefaultPageSize);

        var pagedListQuery = exclude
            ? ctx.Crosstable.GetNotRelatedItems(selectAttributes, filter, sorts, validPagination, [ctx.Id])
            : ctx.Crosstable.GetRelatedItems(selectAttributes, filter, [..sorts], null, validPagination, [ctx.Id]);

        var countQuery = exclude
            ? ctx.Crosstable.GetNotRelatedItemsCount(filter, [ctx.Id])
            : ctx.Crosstable.GetRelatedItemsCount(filter, [ctx.Id]);

        return new ListResult(await queryExecutor.Many(pagedListQuery, token),
            await queryExecutor.Count(countQuery, token));
    }

    private async Task<ListResult?> List(LoadedEntity entity, ImmutableArray<ValidFilter> filters,
        ValidSort[] sorts, Pagination pagination, CancellationToken token)
    {
        var validPagination = PaginationHelper.ToValid(pagination, entity.DefaultPageSize);
        var res = await hookRegistry.EntityPreGetList.Trigger(provider,
            new EntityPreGetListArgs(entity.Name, entity, filters, [..sorts], validPagination));
        var attributes = entity.Attributes.GetLocalAttrs(InListOrDetail.InList);

        var query = entity.ListQuery([..res.RefFilters], [..res.RefSorts], res.RefPagination, null, attributes);

        var records = await queryExecutor.Many(query, token);

        var ret = new ListResult(records, records.Length);
        if (records.Length > 0)
        {
            foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup, InListOrDetail.InList))
            {
                await LoadLookupData(attribute, records, token);
            }

            ret = ret with
            {
                TotalRecords = await queryExecutor.Count(entity.CountQuery(res.RefFilters), token)
            };
        }

        var postRes =
            await hookRegistry.EntityPostGetList.Trigger(provider, new EntityPostGetListArgs(entity.Name, ret));
        return postRes.RefListResult;
    }


    private async Task LoadLookupData(LoadedAttribute attr, Record[] items, CancellationToken token)
    {
        var ids = attr.GetUniq(items);
        if (ids.Length == 0)
        {
            return;
        }

        var lookupEntity = NotNull(attr.Lookup)
            .ValOrThrow($"not find lookup entity from {attr.AddTableModifier()}");

        var query = lookupEntity.ManyQuery(ids,
            [attr.Lookup!.PrimaryKeyAttribute, attr.Lookup!.LoadedTitleAttribute]);
        var targetRecords = await queryExecutor.Many(query, token);
        foreach (var lookupRecord in targetRecords)
        {
            var lookupId = lookupRecord[lookupEntity.PrimaryKey];
            foreach (var item in items.Where(local =>
                         local[attr.Field] is not null && local[attr.Field].Equals(lookupId)))
            {
                item[attr.Field] = lookupRecord;
            }
        }
    }

    private async Task<Record> Update(RecordContext ctx, CancellationToken token)
    {
        var (entity, record) = ctx;
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        Ok(entity.ValidateLocalAttributes(record));
        Ok(entity.ValidateTitleAttributes(record));


        var res = await hookRegistry.EntityPreUpdate.Trigger(provider,
            new EntityPreUpdateArgs(entity.Name, id.ToString()!, record));

        var query = Ok(entity.UpdateQuery(res.RefRecord));
        await queryExecutor.Exec(query, token);
        await hookRegistry.EntityPostUpdate.Trigger(provider,
            new EntityPostUpdateArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    private async Task<Record> Insert(RecordContext ctx, CancellationToken token)
    {
        var (entity, record) = ctx;
        Ok(entity.ValidateLocalAttributes(ctx.Record));
        Ok(ctx.Entity.ValidateTitleAttributes(ctx.Record));

        var res = await hookRegistry.EntityPreAdd.Trigger(provider,
            new EntityPreAddArgs(entity.Name, record));
        record = res.RefRecord;

        var query = entity.Insert(record);
        var id = await queryExecutor.Exec(query, token);
        record[entity.PrimaryKey] = id;

        await hookRegistry.EntityPostAdd.Trigger(provider,
            new EntityPostAddArgs(entity.Name, id.ToString(), record));
        return record;
    }

    private async Task<Record> Delete(RecordContext ctx, CancellationToken token)
    {
        var (entity, record) = ctx;
        if (!record.TryGetValue(entity.PrimaryKey, out var id))
        {
            throw new InvalidParamException("Can not find id ");
        }

        var res = await hookRegistry.EntityPreDel.Trigger(provider,
            new EntityPreDelArgs(entity.Name, id.ToString()!, record));
        record = res.RefRecord;


        var query = Ok(entity.DeleteQuery(record));
        await queryExecutor.Exec(query, token);
        (_, _, record) = await hookRegistry.EntityPostDel.Trigger(provider,
            new EntityPostDelArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    record IdContext(LoadedEntity Entity, object Id);

    private async Task<IdContext> ResolveOneContext(string entityName, string id, CancellationToken token)
    {
        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(entityName, token));
        if (!entitySchemaSvc.ResolveVal(entity.PrimaryKeyAttribute, id, out var idValue))
        {
            throw new InvalidParamException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        return new IdContext(entity, idValue!);
    }

    record CrosstableContext(LoadedAttribute Attribute, Crosstable Crosstable, Object Id);

    private async Task<CrosstableContext> ResolveCrosstableCtx(string entityName, string strId, string attributeName,
        CancellationToken token)
    {
        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(entityName, token));
        var attribute = NotNull(entity.Attributes.FindOneAttr(attributeName))
            .ValOrThrow($"not find {attributeName} in {entityName}");

        var crossTable = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attributeName}");
        if (!entitySchemaSvc.ResolveVal(crossTable.SourceAttribute, strId, out var id))
        {
            throw new InvalidParamException($"Failed to cast {strId} to {crossTable.SourceAttribute.DataType}");
        }

        return new CrosstableContext(attribute, crossTable, id!);
    }

    record RecordContext(LoadedEntity Entity, Record Record);

    private async Task<RecordContext> ResolveRecordContext(string name, JsonElement ele, CancellationToken token)
    {
        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(name, token));
        var record = Ok(entity.Parse(ele, entitySchemaSvc));
        return new RecordContext(entity, record);
    }
}