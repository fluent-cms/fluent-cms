using System.Collections.Immutable;
using FluentCMS.Types;
using FluentCMS.Graph;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Cms.Services;

public sealed class QueryService(
    KateQueryExecutor executor,
    IQuerySchemaService schemaSvc,
    IEntitySchemaService resolver,
    IServiceProvider provider,
    HookRegistry hook
) : IQueryService
{
    public async Task<Record[]> ListWithAction(GraphQlRequestDto dto)
        => await ListWithAction(await FromGraphQlRequest(dto, dto.Args), new Span(),  dto.Args);

    public async Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args, CancellationToken token)
        => await ListWithAction(await FromSavedQuery(name,pagination, !span.IsEmpty(),args,token), span, args, token);

    public async Task<Record?> OneWithAction(GraphQlRequestDto dto)
        => await OneWithAction(await FromGraphQlRequest(dto,dto.Args), dto.Args);

    public async Task<Record?> OneWithAction(string name, StrArgs args, CancellationToken token)
        => await OneWithAction(await FromSavedQuery(name, null, false,args, token),args,token);

    public async Task<Record[]> Partial(string name, string attr, Span span, int limit, StrArgs args,
        CancellationToken token)
    {
        if (span.IsEmpty())
        {
            throw new ResultException("cursor is empty, can not partially execute query");
        }

        var query = await schemaSvc.ByNameAndCache(name, token);
        var attribute = query.Selection.RecursiveFind(attr)?? throw new ResultException("can not find attribute");
        var cross = attribute.Junction ?? throw new ResultException($"can not find Junction of {attribute.Field})");

        var flyPagination = new Pagination(null, limit.ToString());
        var pagination = PaginationHelper.ToValid(flyPagination, attribute.Pagination,
            cross.TargetEntity.DefaultPageSize, true, args);

        var fields = attribute.Selection.GetLocalAttrs();
        var validSpan = span.ToValid(fields, resolver).Ok();

        var filters = FilterHelper.ReplaceVariables(attribute.Filters,args, resolver).Ok();
        var sorts = (await SortHelper.ReplaceVariables(attribute.Sorts, args, cross.TargetEntity, resolver)).Ok(); 
        
        var kateQuery = cross.GetRelatedItems(fields, filters, sorts, validSpan, pagination.PlusLimitOne(),
            [validSpan.SourceId()]);
        var records = await executor.Many(kateQuery, token);

        records = span.ToPage(records, pagination.Limit);
        if (records.Length <= 0) return records;

        await AttachRelated(attribute.Selection, args, records, token);
        var sourceId = records.First()[cross.SourceAttribute.Field];
        SpanHelper.SetSpan(true, attribute.Selection, records, attribute.Sorts, sourceId);
        return records;
    }
    
    private async Task<Record[]> ListWithAction(QueryContext ctx, Span span, StrArgs args,CancellationToken token = default)
    {
        var (query, filters,sorts,pagination) = ctx;
        var validSpan = span.ToValid(query.Entity.Attributes, resolver).Ok();

        var hookParam = new QueryPreGetListArgs(query.Name, query.EntityName, [..filters], query.Sorts, validSpan,
            pagination.PlusLimitOne());
        var res = await hook.QueryPreGetList.Trigger(provider, hookParam);
        if (res.OutRecords is not null)
        {
            return span.ToPage(res.OutRecords, pagination.Limit);
        }

        var kateQuery = query.Entity.ListQuery(filters, sorts, pagination.PlusLimitOne(), validSpan,
            query.Selection.GetLocalAttrs());
        var items = await executor.Many(kateQuery, token);
        items = span.ToPage(items, pagination.Limit);
        if (items.Length <= 0) return items;
        await AttachRelated(query.Selection, args, items, token);

        SpanHelper.SetSpan(true, query.Selection, items, query.Sorts, null);

        return items;
    }

    private async Task<Record?> OneWithAction(QueryContext ctx, StrArgs args, CancellationToken token = default)
    {
        var (query, filters,sorts,_) = ctx;
        var res = await hook.QueryPreGetOne.Trigger(provider,
            new QueryPreGetOneArgs(ctx.Query.Name, query.EntityName, [..filters]));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var kateQuery = query.Entity.OneQuery(filters, sorts, query.Selection.GetLocalAttrs()).Ok();
        var item = await executor.One(kateQuery, token);
        if (item is null) return item;
        await AttachRelated(query.Selection, args, [item], token);
        SpanHelper.SetSpan(false, query.Selection, [item], [], null);
        return item;
    }

    private async Task AttachRelated(ImmutableArray<GraphAttribute>? attrs, StrArgs strArgs, Record[] items,
        CancellationToken token)
    {
        if (attrs is null) return;

        foreach (var attribute in attrs.GetAttrByType<GraphAttribute>(DisplayType.Lookup))
        {
            await AttachLookup(attribute, strArgs, items, token);
        }

        foreach (var attribute in attrs.GetAttrByType<GraphAttribute>(DisplayType.Junction))
        {
            await AttachJunction(attribute, strArgs, items, token);
        }
    }

    private async Task AttachJunction(GraphAttribute attr, StrArgs args, Record[] items, CancellationToken token)
    {
        var cross = attr.Junction ?? throw new ResultException($"not find junction of {attr.AddTableModifier()}");
        var target = cross.TargetEntity;
        //no need to attach, ignore
        var ids = cross.SourceEntity.PrimaryKeyAttribute.GetUniq(items);
        if (ids.Length == 0) return;

        var fields = attr.Selection.GetLocalAttrs();
        var filters = FilterHelper.ReplaceVariables(attr.Filters,args, resolver).Ok();
        var sorts = (await SortHelper.ReplaceVariables(attr.Sorts,args,target, resolver)).Ok();

        var fly = PaginationHelper.ResolvePagination(attr, args) ?? attr.Pagination;
        if (fly.IsEmpty())
        {
            //get all items and no pagination
            var query = cross.GetRelatedItems(fields, filters, [..sorts], null, null, ids);
            var targetRecords = await executor.Many(query, token);
            await AttachRelated(attr.Selection, args, targetRecords, token);
            var targetItemGroups = targetRecords.GroupBy(x => x[cross.SourceAttribute.Field], x => x);
            foreach (var targetGroup in targetItemGroups)
            {
                var parents = items.Where(local => local[cross.SourceEntity.PrimaryKey].Equals(targetGroup.Key));
                foreach (var parent in parents)
                {
                    parent[attr.Field] = targetGroup.ToArray();
                }
            }
        }
        else
        {
            var pagination = PaginationHelper.ToValid(fly, attr.Pagination, target.DefaultPageSize, false, args);
            foreach (var id in ids)
            {
                var query = cross.GetRelatedItems(fields, filters, [..sorts], null, pagination.PlusLimitOne(), [id]);
                var targetRecords = await executor.Many(query, token);

                targetRecords = new Span().ToPage(targetRecords, pagination.Limit);
                if (targetRecords.Length > 0)
                {
                    await AttachRelated(attr.Selection, args, targetRecords,
                        token);
                }

                foreach (var item in items.Where(x => x[cross.JunctionEntity.PrimaryKey].Equals(id.Value)))
                {
                    item[attr.Field] = targetRecords;
                }
            }
        }
    }

    private async Task AttachLookup(GraphAttribute attr, StrArgs strArgs, Record[] items, CancellationToken token)
    {
        var lookupEntity = attr.Lookup??throw new ResultException($"can not find lookup entity of{attr.Field}");

        var selection = attr.Selection.GetLocalAttrs();
        if (selection.FindOneAttr(lookupEntity.PrimaryKey) == null)
        {
            selection = [..selection, lookupEntity.PrimaryKeyAttribute.ToGraph()];
        }

        var ids = attr.GetUniq(items);
        if (ids.Length == 0)
        {
            return;
        }

        var query = lookupEntity.ManyQuery(ids, selection);
        var targetRecords = await executor.Many(query, token);
        await AttachRelated(attr.Selection, strArgs, targetRecords, token);

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

    private record QueryContext(LoadedQuery Query, ValidFilter[] Filters, ValidSort[] Sorts, ValidPagination Pagination);

    private async Task<QueryContext> FromSavedQuery(
        string name, Pagination? pagination,  bool haveCursor, StrArgs args, CancellationToken token =default)
    {
        var query = await schemaSvc.ByNameAndCache(name, token);
        Result.Ok();
        return await GetQueryContext(query, pagination,haveCursor,args);
    }

    private async Task<QueryContext> FromGraphQlRequest(GraphQlRequestDto dto, StrArgs args)
    {
         var loadedQuery = await schemaSvc.ByGraphQlRequest(dto);
         return await GetQueryContext(loadedQuery, null,false,args);
    }

    private async Task<QueryContext> GetQueryContext(LoadedQuery query, Pagination? fly, bool haveCursor, StrArgs args)
    {
        var validPagination = PaginationHelper.ToValid(fly, query.Pagination, query.Entity.DefaultPageSize, haveCursor,args);
        var sort =(await SortHelper.ReplaceVariables(query.Sorts,args, query.Entity, resolver)).Ok();
        var filters = FilterHelper.ReplaceVariables(query.Filters,args, resolver).Ok();
        return new QueryContext(query, filters, sort,validPagination);
    }
}