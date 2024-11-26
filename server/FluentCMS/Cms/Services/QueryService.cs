using System.Collections.Immutable;
using FluentCMS.Services;
using FluentCMS.Utils.Graph;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.Cms.Services;

using static InvalidParamExceptionFactory;

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

    public async Task<Record[]> ManyWithAction(string name, StrArgs args, CancellationToken token)
    {
        var (query, filters,sorts,_) = await FromSavedQuery(name,null,false, args, token);
        var validPagination = new ValidPagination(0, query.Entity.DefaultPageSize);

        var res = await hook.QueryPreGetMany.Trigger(provider,
            new QueryPreGetManyArgs(name, query.EntityName, [..filters], validPagination));
        if (res.OutRecords is not null)
        {
            return res.OutRecords;
        }

        var kateQuery = query.Entity.ListQuery(filters, sorts, validPagination, null,
            query.Selection.GetLocalAttrs());
        var items = await executor.Many(kateQuery, token);
        await AttachRelated(query.Selection, args, items, token);
        SpanHelper.SetSpan(false, query.Selection, items, [], null);
        return items;
    }

    public async Task<Record[]> Partial(string name, string attr, Span span, int limit, StrArgs args,
        CancellationToken token)
    {
        if (span.IsEmpty())
        {
            throw new InvalidParamException("cursor is empty, can not partially execute query");
        }

        var query = await schemaSvc.ByNameAndCache(name, token);
        var attribute = NotNull(query.Selection.RecursiveFind(attr)).ValOrThrow("not find attribute");
        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attribute.Field})");

        var pagination = new ValidPagination(0, limit);

        var validSpan = Ok(span.ToValid([], resolver));
        var fields = attribute.Selection.GetLocalAttrs();

        var filters = Ok( FilterHelper.ReplaceVariables(attribute.Filters,args, resolver));
        var sorts = Ok(await SortHelper.ReplaceVariables(attribute.Sorts, args, cross.TargetEntity, resolver)); 
        
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
        var validSpan = Ok(span.ToValid(query.Entity.Attributes, resolver));

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

        var kateQuery = Ok(query.Entity.OneQuery(filters, sorts, query.Selection.GetLocalAttrs()));
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

        foreach (var attribute in attrs.GetAttrByType<GraphAttribute>(DisplayType.Crosstable))
        {
            await AttachCrosstable(attribute, strArgs, items, token);
        }
    }

    private async Task AttachCrosstable(GraphAttribute attr, StrArgs args, Record[] items, CancellationToken token)
    {
        var cross = NotNull(attr.Crosstable).ValOrThrow($"not find crosstable of {attr.AddTableModifier()}");
        var target = cross.TargetEntity;
        //no need to attach, ignore
        var ids = cross.SourceEntity.PrimaryKeyAttribute.GetUniq(items);
        if (ids.Length == 0) return;

        var fields = attr.Selection.GetLocalAttrs();
        var filters = Ok( FilterHelper.ReplaceVariables(attr.Filters,args, resolver));
        var sorts = Ok(await SortHelper.ReplaceVariables(attr.Sorts,args,target, resolver));

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

                foreach (var item in items.Where(x => x[cross.CrossEntity.PrimaryKey].Equals(id)))
                {
                    item[attr.Field] = targetRecords;
                }
            }
        }
    }

    private async Task AttachLookup(GraphAttribute attr, StrArgs strArgs, Record[] items, CancellationToken token)
    {
        var lookupEntity = NotNull(attr.Lookup).ValOrThrow($"can not find lookup entity of{attr.Field}");

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
        Ok(query.VerifyVariable(args));
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
        var sort =Ok(await SortHelper.ReplaceVariables(query.Sorts,args, query.Entity, resolver));
        var filters = Ok(FilterHelper.ReplaceVariables(query.Filters,args, resolver));
        return new QueryContext(query, filters, sort,validPagination);
    }
}