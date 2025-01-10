using System.Text.Json;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Core.HookFactory;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Core.Descriptors;
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
        var entity = (await entitySchemaSvc.LoadEntity(name, ct)).Ok();
        var (filters, sorts,validPagination) = await GetListArgs(entity, args,pagination);
        return await ListWithAction(entity,mode, filters, sorts, validPagination, ct);
    }

    public async Task<Record[]> ListAsTree(string name, CancellationToken ct)
    {
        var entity = await entitySchemaSvc.LoadEntity(name, ct).Ok();
        var parentField = entity.Attributes.FirstOrDefault(x =>
            x.DataType == DataType.Collection && x.GetCollectionTarget(out var entityName, out _) && entityName == name
        )?? throw new ResultException("Can not compose list result as tree, not find an collection attribute whose target is the entity.");
        
        parentField.GetCollectionTarget(out _, out var linkField);
        var attributes = entity.Attributes.Where(x=>x.Field ==entity.PrimaryKey || x.InList && x.IsLocal());
        var items = await queryExecutor.Many(entity.AllQuery(attributes),ct);
        return items.ToTree(entity.PrimaryKey, linkField);
    }
    
    public async Task<Record> SingleByIdBasic(string entityName, string id, string[] attributes,
        CancellationToken ct)
    {
        var ctx = await GetIdCtx(entityName, id, ct);
        var query = ctx.Entity.ByIdsQuery( ctx.Entity.Attributes.Where(x=>x.IsLocal() && attributes.Contains(x.Field)),[ctx.Id]);
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

        var query = ctx.Entity.ByIdsQuery(ctx.Entity.Attributes.Where(x=> x.Field == ctx.Entity.PrimaryKey || x.InDetail && x.IsLocal()), [ctx.Id]);
        var record = await queryExecutor.One(query, ct) ??
                     throw new ResultException($"not find record by [{id}]");

        foreach (var attribute in ctx.Entity.Attributes.Where(x=>x is { DataType: DataType.Lookup, InList: true }))
        {
            await LoadLookupData(attribute, [record], ct);
        }

        await hookRegistry.EntityPostGetSingle.Trigger(provider, new EntityPostGetSingleArgs(entityName, id, record));
        return record;
    }

    public async Task<Record> InsertWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await InsertWithAction(await GetRecordCtx(name, ele, ct), ct);
    }

    public async Task BatchInsert(string tableName, Record[] items)
    {
        var cols = items[0].Select(x => x.Key);
        var values = items.Select(item => item.Select(kv => kv.Value));
        var query = new SqlKata.Query(tableName).AsInsert(cols, values);
        await queryExecutor.Exec(query);
    }

    public async Task<Record> UpdateWithAction(string name, JsonElement ele, CancellationToken ct)
    {
        return await UpdateWithAction(await GetRecordCtx(name, ele, ct), ct);
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

    public async Task<int> JunctionSave(string name, string id, string attr, JsonElement[] elements,
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

    public async Task<ListResponse> JunctionList(string name, string sid, string attr, bool exclude,
        Pagination pagination,
        StrArgs args, CancellationToken ct)
    {
        var (_, junction, id) = await GetJunctionCtx(name, sid, attr, ct);
        var target = junction.TargetEntity;

        var selectAttributes = target.Attributes.Where(x=>x.Field == target.PrimaryKey || x.IsLocal() && x.InList);

        var (filters, sorts, validPagination) = await GetListArgs(target, args, pagination);

        var listQuery = exclude
            ? junction.GetNotRelatedItems(selectAttributes, filters, sorts, validPagination, [id])
            : junction.GetRelatedItems(filters, [..sorts], validPagination, null, selectAttributes, [id]);

        var countQuery = exclude
            ? junction.GetNotRelatedItemsCount(filters, [id])
            : junction.GetRelatedItemsCount(filters, [id]);

        return new ListResponse(await GetItemsAndInListLookups(junction.TargetEntity, listQuery, ct),
            await queryExecutor.Count(countQuery, ct));
    }

    public async Task<Record> CollectionInsert(string name, string sid, string attr, JsonElement element, CancellationToken ct = default)
    {
        var (collection,id) = await GetCollectionCtx(name, sid, attr, ct);
        var item = collection.TargetEntity.Parse(element, entitySchemaSvc).Ok();
        item[collection.LinkAttribute.Field] = id.ObjectValue!;
        return await InsertWithAction(new RecordContext(collection.TargetEntity, item), ct);
    }

    public async Task<ListResponse> CollectionList(string name, string sid, string attr, Pagination pagination, StrArgs args, CancellationToken ct = default)
    {
        var (collection,id) = await GetCollectionCtx(name, sid, attr, ct);
        var (filters, sorts,validPagination) = await GetListArgs(collection.TargetEntity, args,pagination);

        var attributes =
            collection.TargetEntity.Attributes.Where(x=> x.Field == collection.TargetEntity.PrimaryKey || x.IsLocal() && x.InList);    
        var listQuery = collection.List(filters,sorts,validPagination,null,attributes,[id]);
      
        var countQuery = collection.Count(filters,[id]);
        return new ListResponse(
            await GetItemsAndInListLookups(collection.TargetEntity, listQuery, ct),
            await queryExecutor.Count(countQuery, ct));
    }

    
    private async Task<ListResponse?> ListWithAction(
        
        LoadedEntity entity, 
        ListResponseMode mode,
        ValidFilter[] filters,
        ValidSort[] sorts, 
        ValidPagination pagination, 
        CancellationToken ct)
    {
        var args = new EntityPreGetListArgs(
            Entity: entity,
            RefFilters: [..filters],
            RefSorts: [..sorts],
            RefPagination: pagination,
            ListResponseMode: mode
        );

        var res = await hookRegistry.EntityPreGetList.Trigger(provider, args);
        var attributes = entity.Attributes.Where(x=>x.Field ==entity.PrimaryKey || x.InList && x.IsLocal());

        var listQuery = entity.ListQuery([..res.RefFilters], [..res.RefSorts], res.RefPagination, null, attributes);
        var countQuery = entity.CountQuery([..res.RefFilters]);
        var ret = mode switch
        {
            ListResponseMode.Count => new ListResponse([], await queryExecutor.Count(countQuery, ct)),
            ListResponseMode.Items => new ListResponse(await GetItemsAndInListLookups(entity, listQuery, ct), 0),
            _ => new ListResponse(await GetItemsAndInListLookups(entity,listQuery,ct), await queryExecutor.Count(countQuery, ct))
        };

        var postArgs = new EntityPostGetListArgs(Entity: entity, RefListResponse: ret);
        var postRes = await hookRegistry.EntityPostGetList.Trigger(provider, postArgs);
        return postRes.RefListResponse;
    }

    async Task<Record[]> GetItemsAndInListLookups(LoadedEntity entity, SqlKata.Query query, CancellationToken ct)
    {
        var items = await queryExecutor.Many(query, ct);
        if (items.Length == 0) return items;
        foreach (var attribute in entity.Attributes.Where(x=>x is { DataType: DataType.Lookup, InList: true }))
        {
            await LoadLookupData(attribute, items, ct);
        }

        return items;
    } 

    private async Task LoadLookupData(LoadedAttribute attr, Record[] items, CancellationToken token)
    {
        var ids = attr.GetUniq(items);
        if (ids.Length == 0) return;

        var lookup = attr.Lookup ??
              throw new ResultException($"not find lookup entity from {attr.AddTableModifier()}");

        var query = lookup.LookupTitleQuery(ids);
        
        var targetRecords = await queryExecutor.Many(query, token);
        foreach (var lookupRecord in targetRecords)
        {
            var lookupId = lookupRecord[lookup.TargetEntity.PrimaryKey];
            foreach (var item in items.Where(local =>
                         local[attr.Field] is not null && local[attr.Field].Equals(lookupId)))
            {
                item[attr.Field] = lookupRecord;
            }
        }
    }

    private async Task<Record> UpdateWithAction(RecordContext ctx, CancellationToken token)
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

    private async Task<Record> InsertWithAction(RecordContext ctx, CancellationToken token)
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
        var entity = (await entitySchemaSvc.LoadEntity(entityName, token)).Ok();
        if (!entitySchemaSvc.ResolveVal(entity.PrimaryKeyAttribute, id, out var idValue))
        {
            throw new ResultException($"Failed to cast {id} to {entity.PrimaryKeyAttribute.DataType}");
        }

        return new IdContext(entity, idValue!.Value);
    }
    
    private record CollectionContext(Collection Collection, ValidValue Id);

    private async Task<CollectionContext> GetCollectionCtx(string entity, string sid, string attr, CancellationToken ct)
    {
        var loadedEntity = (await entitySchemaSvc.LoadEntity(entity, ct)).Ok();
        var collection = loadedEntity.Attributes.FirstOrDefault(x=>x.Field ==attr)?.Collection ??
                        throw new ResultException($"Failed to get Collection Context, cannot find [{attr}] in [{entity}]");

        if (!entitySchemaSvc.ResolveVal(loadedEntity.PrimaryKeyAttribute, sid, out var id))
        {
            throw new ResultException($"Failed to cast {sid} to {loadedEntity.PrimaryKeyAttribute.DataType}");
        }
        return new CollectionContext( collection, id!.Value);
    }


    private record JunctionContext(LoadedAttribute Attribute, Junction Junction, ValidValue Id);
    

    private async Task<JunctionContext> GetJunctionCtx(string entity, string sid, string attr, CancellationToken ct)
    {
        var loadedEntity = (await entitySchemaSvc.LoadEntity(entity, ct)).Ok();
        var errMessage = $"Failed to Get Junction Context, cannot find [{attr}] in [{entity}]";
        var attribute = loadedEntity.Attributes.FirstOrDefault(x=>x.Field == attr) ??
                        throw new ResultException(errMessage);

        var junction = attribute.Junction ?? throw new ResultException(errMessage);
        if (!entitySchemaSvc.ResolveVal(junction.SourceAttribute, sid, out var id))
        {
            throw new ResultException($"Failed to cast {sid} to {junction.SourceAttribute.DataType}");
        }

        return new JunctionContext(attribute, junction, id!.Value);
    }

    private record RecordContext(LoadedEntity Entity, Record Record);
    private async Task<RecordContext> GetRecordCtx(string name, JsonElement ele, CancellationToken token)
    {
        var entity = (await entitySchemaSvc.LoadEntity(name, token)).Ok();
        var record = entity.Parse(ele, entitySchemaSvc).Ok();
        return new RecordContext(entity, record);
    }

    private record LookupContext(LoadedEntity Entity, ValidSort[] Sorts, ValidPagination Pagination, LoadedAttribute[] Attributes);

    private async Task<LookupContext> GetLookupContext(string name, CancellationToken ct = default)
    {
        var entity = (await entitySchemaSvc.LoadEntity(name, ct)).Ok();
        var sort = new Sort(entity.TitleAttribute, SortOrder.Asc);
        var validSort = (await SortHelper.ToValidSorts([sort], entity, entitySchemaSvc)).Ok();
        var pagination = PaginationHelper.ToValid(new Pagination(), entity.DefaultPageSize);
        return new LookupContext(entity, validSort, pagination,[entity.PrimaryKeyAttribute,entity.LoadedTitleAttribute]);
    }

    private record ListArgs(ValidFilter[] Filters, ValidSort[] Sorts, ValidPagination Pagination);
    private async Task<ListArgs> GetListArgs(LoadedEntity entity,  StrArgs args,Pagination pagination)
    {
        var groupedArgs = args.GroupByFirstIdentifier();
        var filters = (await QueryStringFilterResolver.Resolve(entity, groupedArgs, entitySchemaSvc, entitySchemaSvc)).Ok();
        var sorts = (await SortHelper.Parse(entity, groupedArgs, entitySchemaSvc)).Ok();
        var validPagination = PaginationHelper.ToValid(pagination, entity.DefaultPageSize);
        return new ListArgs(filters, sorts, validPagination);
    }
}