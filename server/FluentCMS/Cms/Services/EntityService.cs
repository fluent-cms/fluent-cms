using System.Collections.Immutable;
using System.Text.Json;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Cms.Services;

public sealed class EntityService(
    IServiceProvider provider,
    KateQueryExecutor queryExecutor,
    IEntitySchemaService entitySchemaSvc,
    HookRegistry hookRegistry
) : IEntityService
{

    public async Task<ListResponse?> ListWithAction(
        string name, 
        ListResponseMode mode, 
        Pagination pagination,
        StrArgs args, 
        CancellationToken ct)
    {
        var entity = (await entitySchemaSvc.GetLoadedEntity(name, ct)).Ok();
        var groupedArgs = args.GroupByFirstIdentifier();

        var filters = (await FilterHelper.Parse(entity, groupedArgs, entitySchemaSvc, entitySchemaSvc)).Ok();
        var sorts = (await SortHelper.Parse(entity, groupedArgs, entitySchemaSvc)).Ok();
        return await ListWithAction(entity,mode, [..filters], [..sorts], pagination, ct);
    }

    public async Task<Record> SingleByIdBasic(string entityName, string id, string[] attributes,
        CancellationToken ct)
    {
        var ctx = await GetIdCtx(entityName, id, ct);
        var query = ctx.Entity.ByIdQuery(ctx.Id, ctx.Entity.Attributes.GetLocalAttrs(attributes), []);
        return await queryExecutor.One(query, ct) ??
               throw new ResultException($"not find record by [{id}]");
    }

    public async Task<Record> SingleWithAction(string entityName, string id, CancellationToken ct = default)
    {
        var ctx = await GetIdCtx(entityName, id, ct);
        var res = await hookRegistry.EntityPreGetSingle.Trigger(provider,
            new EntityPreGetSingleArgs(entityName, id, null));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var query = ctx.Entity.ByIdQuery(ctx.Id,
            ctx.Entity.Attributes.GetLocalAttrs(ctx.Entity.PrimaryKey, InListOrDetail.InDetail), []);
        var record = await queryExecutor.One(query, ct) ??
                     throw new ResultException($"not find record by [{id}]");

        foreach (var attribute in ctx.Entity.Attributes.GetAttrByType(DataType.Lookup, InListOrDetail.InDetail))
        {
            await LoadLookupData(attribute, [record], ct);
        }

        await hookRegistry.EntityPostGetSingle.Trigger(provider, new EntityPostGetSingleArgs(entityName, id, record));
        return record;
    }


    public async Task<Record> InsertWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await Insert(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task BatchInsert(string tableName, IEnumerable<string> cols, IEnumerable<IEnumerable<object>> items)
    {
        var query = new SqlKata.Query(tableName).AsInsert(cols, items);
        await queryExecutor.Exec(query);
    }

    public async Task<Record> UpdateWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await Update(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task<Record> DeleteWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await Delete(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task<LookupListResponse> LookupList(string name, string startsVal, CancellationToken ct = default)
    {
        var (entity, sorts, pagination, attributes) = await GetLookupContext(name, ct);
        var count = await queryExecutor.Count(entity.CountQuery([]), ct);
        if (count < entity.DefaultPageSize)
        {
            //not enough for one page, search in client
            var query = entity.ListQuery([], sorts, pagination, null, attributes);
            var items = await queryExecutor.Many(query, ct);
            return new LookupListResponse(false, items);
        }

        ValidFilter[] filters = [];
        if (!string.IsNullOrEmpty(startsVal))
        {
            var constraint = new Constraint(Matches.StartsWith, [startsVal]);
            var filter = new Filter(entity.TitleAttribute, MatchTypes.MatchAll, [constraint]);
            filters = (await FilterHelper.ToValidFilters([filter], entity, entitySchemaSvc, entitySchemaSvc)).Ok();
        }

        var queryWithFilters = entity.ListQuery(filters, sorts, pagination, null, attributes);
        var filteredItems = await queryExecutor.Many(queryWithFilters, ct);
        return new LookupListResponse(true, filteredItems);
    }

    public async Task<int> JunctionDelete(string name, string id, string attr, JsonElement[] elements,
        CancellationToken ct)
    {
        var ctx = await GetJunctionCtx(name, id, attr, ct);
        var items = elements.Select(ele =>
            ctx.Junction.TargetEntity.Parse(ele, entitySchemaSvc).Ok()).ToArray();

        var res = await hookRegistry.JunctionPreDel.Trigger(provider,
            new JunctionPreDelArgs(name, id, ctx.Attribute, items));

        var query = ctx.Junction.Delete(ctx.Id, res.RefItems);
        var ret = await queryExecutor.Exec(query, ct);
        await hookRegistry.JunctionPostDel.Trigger(provider,
            new JunctionPostDelArgs(name, id, ctx.Attribute, items));
        return ret;
    }

    public async Task<int> JunctionAdd(string name, string id, string attr, JsonElement[] elements,
        CancellationToken ct)
    {
        var ctx = await GetJunctionCtx(name, id, attr, ct);

        var items = elements
            .Select(ele => ctx.Junction.TargetEntity.Parse(ele, entitySchemaSvc).Ok()).ToArray();
        var res = await hookRegistry.JunctionPreAdd.Trigger(provider,
            new JunctionPreAddArgs(name, id, ctx.Attribute, items));
        var query = ctx.Junction.Insert(ctx.Id, res.RefItems);

        var ret = await queryExecutor.Exec(query, ct);
        await hookRegistry.JunctionPostAdd.Trigger(provider,
            new JunctionPostAddArgs(name, id, ctx.Attribute, items));
        return ret;
    }


    public async Task<ListResponse> JunctionList(string name, string id, string attr, bool exclude,
        StrArgs args, Pagination pagination, CancellationToken ct)
    {
        var ctx = await GetJunctionCtx(name, id, attr, ct);
        var target = ctx.Junction.TargetEntity;

        var selectAttributes = target.Attributes.GetLocalAttrs(target.PrimaryKey, InListOrDetail.InList);

        var dictionary = args.GroupByFirstIdentifier();
        var filter = (await FilterHelper.Parse(target, dictionary, entitySchemaSvc, entitySchemaSvc)).Ok();
        var sorts = (await SortHelper.Parse(target, dictionary, entitySchemaSvc)).Ok();
        var validPagination = PaginationHelper.ToValid(pagination, target.DefaultPageSize);

        var pagedListQuery = exclude
            ? ctx.Junction.GetNotRelatedItems(selectAttributes, filter, sorts, validPagination, [ctx.Id])
            : ctx.Junction.GetRelatedItems(selectAttributes, filter, [..sorts], null, validPagination, [ctx.Id]);

        var countQuery = exclude
            ? ctx.Junction.GetNotRelatedItemsCount(filter, [ctx.Id])
            : ctx.Junction.GetRelatedItemsCount(filter, [ctx.Id]);

        return new ListResponse(await queryExecutor.Many(pagedListQuery, ct),
            await queryExecutor.Count(countQuery, ct));
    }

    private async Task<ListResponse?> ListWithAction(
        LoadedEntity entity, 
        ListResponseMode mode,
        ImmutableArray<ValidFilter> filters,
        ValidSort[] sorts, 
        Pagination pagination, 
         CancellationToken token)
    {
        var validPagination = PaginationHelper.ToValid(pagination, entity.DefaultPageSize);
        var args = new EntityPreGetListArgs(
            Entity: entity,
            RefFilters: filters,
            RefSorts: [..sorts],
            RefPagination: validPagination,
            ListResponseMode: mode
        );

        var res = await hookRegistry.EntityPreGetList.Trigger(provider, args);
        var attributes = entity.Attributes.GetLocalAttrs(entity.PrimaryKey, InListOrDetail.InList);

        var query = entity.ListQuery([..res.RefFilters], [..res.RefSorts], res.RefPagination, null, attributes);
        var ret = mode switch
        {
            ListResponseMode.count => new ListResponse([], await queryExecutor.Count(query, token)),
            ListResponseMode.items => new ListResponse(await GetItems(), 0),
            _ => new ListResponse(await GetItems(), await queryExecutor.Count(query, token))
        };

        var postArgs = new EntityPostGetListArgs(Entity: entity, RefListResponse: ret);
        var postRes = await hookRegistry.EntityPostGetList.Trigger(provider, postArgs);
        return postRes.RefListResponse;

        async Task<Record[]> GetItems()
        {
            var items = await queryExecutor.Many(query, token);
            if (items.Length == 0) return items;
            foreach (var attribute in entity.Attributes.GetAttrByType(DataType.Lookup, InListOrDetail.InList))
            {
                await LoadLookupData(attribute, items, token);
            }
            return items;   
        }
    }


    private async Task LoadLookupData(LoadedAttribute attr, Record[] items, CancellationToken token)
    {
        var ids = attr.GetUniq(items);
        if (ids.Length == 0)
        {
            return;
        }

        var lookupEntity = attr.Lookup ??
                           throw new ResultException($"not find lookup entity from {attr.AddTableModifier()}");

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
            throw new ResultException("Can not find id ");
        }

        entity.ValidateLocalAttributes(record).Ok();
        entity.ValidateTitleAttributes(record).Ok();

        var res = await hookRegistry.EntityPreUpdate.Trigger(provider,
            new EntityPreUpdateArgs(entity.Name, id.ToString()!, record));

        var query = entity.UpdateQuery(res.RefRecord).Ok();
        await queryExecutor.Exec(query, token);
        await hookRegistry.EntityPostUpdate.Trigger(provider,
            new EntityPostUpdateArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    private async Task<Record> Insert(RecordContext ctx, CancellationToken token)
    {
        var (entity, record) = ctx;
        entity.ValidateLocalAttributes(record).Ok();
        entity.ValidateTitleAttributes(record).Ok();

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
            throw new ResultException("Can not find id ");
        }

        var res = await hookRegistry.EntityPreDel.Trigger(provider,
            new EntityPreDelArgs(entity.Name, id.ToString()!, record));
        record = res.RefRecord;


        var query = entity.DeleteQuery(record).Ok();
        await queryExecutor.Exec(query, token);
        (_, _, record) = await hookRegistry.EntityPostDel.Trigger(provider,
            new EntityPostDelArgs(entity.Name, id.ToString()!, record));
        return record;
    }

    record IdContext(LoadedEntity Entity, ValidValue Id);

    private async Task<IdContext> GetIdCtx(string entityName, string id, CancellationToken token)
    {
        var entity = (await entitySchemaSvc.GetLoadedEntity(entityName, token)).Ok();
        if (!entitySchemaSvc.ResolveVal(entity.PrimaryKeyAttribute, id, out var idValue))
        {
            throw new ResultException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        return new IdContext(entity, idValue);
    }

    record JunctionContext(LoadedAttribute Attribute, Junction Junction, ValidValue Id);

    private async Task<JunctionContext> GetJunctionCtx(string entityName, string strId, string attributeName,
        CancellationToken token)
    {
        var entity = (await entitySchemaSvc.GetLoadedEntity(entityName, token)).Ok();
        var attribute = entity.Attributes.FindOneAttr(attributeName) ??
                        throw new ResultException($"not find {attributeName} in {entityName}");

        var junction = attribute.Junction ?? throw new ResultException($"not find Junction of {attributeName}");
        if (!entitySchemaSvc.ResolveVal(junction.SourceAttribute, strId, out var id))
        {
            throw new ResultException($"Failed to cast {strId} to {junction.SourceAttribute.DataType}");
        }

        return new JunctionContext(attribute, junction, id);
    }

    record RecordContext(LoadedEntity Entity, Record Record);
    private async Task<RecordContext> GetRecordCtx(string name, JsonElement ele, CancellationToken token)
    {
        var entity = (await entitySchemaSvc.GetLoadedEntity(name, token)).Ok();
        var record = entity.Parse(ele, entitySchemaSvc).Ok();
        return new RecordContext(entity, record);
    }

    private record LookupContext(LoadedEntity Entity, ValidSort[] Sorts, ValidPagination Pagination, LoadedAttribute[] Attributes);

    private async Task<LookupContext> GetLookupContext(string name, CancellationToken ct = default)
    {
        var entity = (await entitySchemaSvc.GetLoadedEntity(name, ct)).Ok();
        var sort = new Sort(entity.TitleAttribute, SortOrder.Asc);
        var validSort = (await SortHelper.ToValidSorts([sort], entity, entitySchemaSvc)).Ok();
        var pagination = PaginationHelper.ToValid(new Pagination(), entity.DefaultPageSize);
        return new LookupContext(entity, validSort, pagination,[entity.PrimaryKeyAttribute,entity.LoadedTitleAttribute]);
    }
}