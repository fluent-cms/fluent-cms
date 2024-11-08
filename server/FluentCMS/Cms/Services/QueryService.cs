using System.Collections.Immutable;
using FluentCMS.Services;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.HookFactory;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

using static InvalidParamExceptionFactory;

public sealed class QueryService(
    KateQueryExecutor executor,
    IEntitySchemaService entitySchema,
    IQuerySchemaService querySchema,
    IServiceProvider provider,
    HookRegistry hook) : IQueryService
{

    public async Task<Record[]> Partial(string name, string attr,  Span span, int limit,QueryArgs args, CancellationToken token)
    {
        if (span.IsEmpty())
        {
            throw new InvalidParamException("cursor is empty, can not partially execute query");
        }

        var query = await querySchema.GetByNameAndCache(name, token);
        var attribute = NotNull(query.Selection.RecursiveFind(attr)).ValOrThrow("not find attribute");
        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attribute.Field})");
        
        var pagination = new Pagination(0, limit).ToValid(cross.TargetEntity.DefaultPageSize);
        
        var validSpan =CheckResult(span.ToValid([]));
        var fields = attribute.Selection.GetLocalAttrs();
        var filters = CheckResult(await attribute.Filters.ToValid(cross.TargetEntity, args, entitySchema.ResolveAttributeVector));
        var sorts = CheckResult(await attribute.Sorts.ToValidSorts(cross.TargetEntity, entitySchema.ResolveAttributeVector));
        
        var kateQuery = cross.GetRelatedItems(fields, filters,sorts, validSpan, pagination.PlusLimitOne(), [validSpan.SourceId()]);
        var records = await executor.Many(kateQuery, token);

        records = span.ToPage(records, pagination.Limit);
        if (records.Length <= 0) return records;
        
        await AttachRelated(attribute.Selection, args, records, token);
        var sourceId = records.First()[attribute.Crosstable!.SourceAttribute.Field];
        SetSpan(true, attribute.Selection, records, attribute.Sorts, sourceId);
        return records;
    }

    public async Task<Record[]> List(string name, Span span, Pagination pagination, QueryArgs args, CancellationToken token)
    {
        var (query,selection,filters) = await GetContext(name, args, token);

        var validSpan = CheckResult(span.ToValid(query.Entity.Attributes));
        
        if (!span.IsEmpty())
        {
            pagination = pagination with { Offset = 0 };
        }
        var validPagination = pagination.ToValid(query.PageSize);
        
        var hookParam = new QueryPreGetListArgs(name, query.EntityName, filters, query.Sorts, validSpan, validPagination.PlusLimitOne());
        var res = await hook.QueryPreGetList.Trigger(provider, hookParam);
        if (res.OutRecords is not null)
        {
            return span.ToPage(res.OutRecords, validPagination.Limit); 
        }

        var kateQuery = query.Entity.ListQuery(filters, query.Sorts, validPagination.PlusLimitOne(), validSpan, selection);
        var items = await executor.Many(kateQuery, token);
        items = span.ToPage(items, validPagination.Limit);
        if (items.Length <= 0) return items;
        await AttachRelated(query.Selection, args, items, token);
        
        SetSpan(true,query.Selection, items, query.Sorts, null);

        return items;
    }
    
    public async Task<Record[]> Many(string name, QueryArgs args, CancellationToken token)
    {
        var (query,selection,filters) = await GetContext(name, args, token);
        var validPagination = new Pagination().ToValid(query.PageSize);

        var res = await hook.QueryPreGetMany.Trigger(provider,
            new QueryPreGetManyArgs(name, query.EntityName, filters, validPagination));
        if (res.OutRecords is not null)
        {
            return res.OutRecords;
        }
      
        var kateQuery = query.Entity.ListQuery(res.Filters, query.Sorts, validPagination, null, selection);
        var items = await executor.Many(kateQuery, token);
        await AttachRelated(query.Selection, args, items, token);
        SetSpan(false,query.Selection, items, [], null);
        return items;
    }

    public async Task<Record> One(string name, Dictionary<string, StringValues> args, CancellationToken token)
    {
        var (query,selection,filters) = await GetContext(name, args, token);
        var res = await hook.QueryPreGetOne.Trigger(provider,
            new QueryPreGetOneArgs(name, query.EntityName, filters));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }
      
        var kateQuery = CheckResult(query.Entity.OneQuery(res.Filters, query.Sorts, selection));
        var item = NotNull(await executor.One(kateQuery, token)).ValOrThrow("Not find record");
        await AttachRelated(query.Selection, args, [item], token);
        SetSpan(false, query.Selection, [item], [],null);
        return item;
    }

    private async Task AttachRelated(ImmutableArray<GraphAttribute>? attrs, QueryArgs args, Record[] items, CancellationToken token)
    {
        if (attrs is null) return;

        foreach (var attribute in attrs.GetAttrByType<GraphAttribute>(DisplayType.Lookup))
        {
            await AttachLookup(attribute, args, items, token);
        }

        foreach (var attribute in attrs.GetAttrByType<GraphAttribute>(DisplayType.Crosstable))
        {
            await AttachCrosstable(attribute, args, items, token);
        }
    }

    private async Task AttachCrosstable(GraphAttribute attr, QueryArgs args, Record[] items, CancellationToken token)
    {
        var cross = NotNull(attr.Crosstable).ValOrThrow($"not find crosstable of {attr.AddTableModifier()}");
        //no need to attach, ignore
        var ids = cross.SourceEntity.PrimaryKeyAttribute.GetUniq(items);
        if (ids.Length == 0) return;

        var fields = attr.Selection.GetLocalAttrs();
        var filters = CheckResult(await attr.Filters.ToValid(cross.TargetEntity, args,
            entitySchema.ResolveAttributeVector));
        var sorts = CheckResult(await attr.Sorts.ToValidSorts(attr.Crosstable!.TargetEntity,
            entitySchema.ResolveAttributeVector));

        var pagination = PaginationHelper.ResolvePagination(attr,args);

        if (pagination is null)
        {
            //get all items and no pagination
            var query = cross.GetRelatedItems(fields, filters, sorts, null,null, ids);
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
            foreach (var id in ids)
            {
                var validPagination = pagination.ToValid(cross.TargetEntity.DefaultPageSize);

                var query = cross.GetRelatedItems(fields, filters, sorts, null,validPagination.PlusLimitOne(), [id]);
                var targetRecords = await executor.Many(query, token);

                targetRecords = new Span().ToPage(targetRecords, validPagination.Limit);
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

    private async Task AttachLookup(GraphAttribute attr, QueryArgs args, Record[] items, CancellationToken token)
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
        await AttachRelated(attr.Selection, args, targetRecords, token);

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
    
    private static void SetSpan(bool needAddCursor,  ImmutableArray<GraphAttribute> attrs, Record[] items,  IEnumerable<ValidSort> sorts, object? sourceId)
    {
        var arr = sorts.ToArray();
        if (needAddCursor)
        {
            if (items.Length == 0) return;
            SpanHelper.SetCursor(sourceId, items.First(), arr);
            if (items.Length > 1) SpanHelper.SetCursor(sourceId, items.Last(), arr);
        }

        foreach (var item in items)
        {
            foreach (var attribute in attrs.GetAttrByType(DisplayType.Lookup))
            {
                if (item.TryGetValue(attribute.Field, out var value) && value is  Record record)
                {
                    SetSpan( false, attribute.Selection, [record],   [], null);
                }
            }

            foreach (var attribute in attrs.GetAttrByType(DisplayType.Crosstable))
            {
                if (!item.TryGetValue(attribute.Field, out var value) || value is not Record[] records || records.Length <= 0) continue;
                var nextSourceId = records.First()[attribute.Crosstable!.SourceAttribute.Field];
                SetSpan(true,attribute.Selection,  records, attribute.Sorts, nextSourceId);
            }
        }
    }

    private record Context(LoadedQuery Query, ImmutableArray<GraphAttribute> Selection, ImmutableArray<ValidFilter> Filters);

    private async Task<Context> GetContext(string name, QueryArgs args, CancellationToken token)
    {
        var query = await querySchema.GetByNameAndCache(name, token);
        var attributes = query.Selection.GetLocalAttrs();
        var filters = CheckResult(await query.Filters.ToValid(query.Entity, args, entitySchema.ResolveAttributeVector));
        return new Context(query, attributes, filters);
    }
}