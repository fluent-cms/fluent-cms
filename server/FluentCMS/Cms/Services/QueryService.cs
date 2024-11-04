using System.Collections.Immutable;
using FluentCMS.Services;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.HookFactory;
namespace FluentCMS.Cms.Services;

using static InvalidParamExceptionFactory;

public sealed class QueryService(
    KateQueryExecutor kateQueryExecutor,
    IEntitySchemaService entitySchemaService,
    IQuerySchemaService querySchemaService,
    IEntityService entityService,
    IServiceProvider provider,
    HookRegistry hookRegistry
) : IQueryService
{


    /* e.g. for query course:
     * attrPath query teacher.skills
     * item: {id : 4, teacher.id:,2}
     */
    public async Task<Record> Partial(string queryName, string attrPath, Cursor cursor, int limit, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    // todo : for crosstable item, also return cursor : first, last;
    // todo: pagination also put into qsDictionary, e.g. teacher.skill.offset = 2, teacher.skill.limit = 4
    public async Task<Record[]> List(string queryName,
        Cursor cursor,
        Pagination pagination,
        Dictionary<string, StringValues> filterArgs,
        Dictionary<string, StringValues> paginationArgs,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var filters = CheckResult(await query.Filters.ToValid(
            query.Entity,
            filterArgs,
            entitySchemaService.ResolveAttributeVector));
        var parsedCursor = CheckResult(cursor.Resolve(query.Entity));

        if (!cursor.IsEmpty())
        {
            pagination = pagination with { Offset = 0 };
        }

        var validPagination = pagination.ToValid(query.PageSize);

        validPagination = validPagination with { Limit = validPagination.Limit + 1 }; // add extra to check has more
        var sorts = CheckResult(
            await query.Sorts.ToValidSorts(query.Entity, entitySchemaService.ResolveAttributeVector));
        var hookParam =
            new QueryPreGetListArgs(queryName, query.EntityName, filters, sorts, parsedCursor, validPagination);
        var res = await hookRegistry.QueryPreGetList.Trigger(provider, hookParam);
        if (res.OutRecords is not null)
        {
            return SetCursor(res.OutRecords, cursor, validPagination, sorts);
        }

        var attributes = query.Selection.GetLocalAttributes();
        var kateQuery = CheckResult(query.Entity.ListQuery(filters, sorts, validPagination, parsedCursor, attributes));
        var items = await kateQueryExecutor.Many(kateQuery, cancellationToken);

        if (!cursor.IsForward())
        {
            items = items.Reverse().ToArray();
        }

        items = SetCursor(items, cursor, validPagination, sorts);
        if (items.Length > 0)
        {
            await AttachRelatedEntity(query.Selection, paginationArgs, filterArgs, items, cancellationToken);
        }

        return items;
    }

    public async Task<Record[]> Many(string queryName,
        Dictionary<string, StringValues> filterArgs,
        Dictionary<string, StringValues> paginationArgs,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var validPagination = new Pagination().ToValid(query.PageSize);
        var filters = CheckResult(await query.Filters.ToValid(
            query.Entity, filterArgs, entitySchemaService.ResolveAttributeVector));

        var res = await hookRegistry.QueryPreGetMany.Trigger(provider,
            new QueryPreGetManyArgs(queryName, query.EntityName, filters, validPagination));
        if (res.OutRecords is not null)
        {
            return res.OutRecords;
        }

        var attributes = query.Selection.GetLocalAttributes();
        var sorts = CheckResult(
            await query.Sorts.ToValidSorts(query.Entity, entitySchemaService.ResolveAttributeVector));
        var kateQuery = CheckResult(query.Entity.ListQuery(res.Filters, sorts, validPagination, null, attributes));
        var items = await kateQueryExecutor.Many(kateQuery, cancellationToken);
        await AttachRelatedEntity(query.Selection,paginationArgs, filterArgs, items, cancellationToken);
        return items;
    }

    public async Task<Record> One(string queryName, 
        Dictionary<string, StringValues> filterArgs,
        Dictionary<string, StringValues> paginationArgs,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var filters = CheckResult(await query.Filters.ToValid(
            query.Entity, filterArgs, entitySchemaService.ResolveAttributeVector));
        var res = await hookRegistry.QueryPreGetOne.Trigger(provider,
            new QueryPreGetOneArgs(queryName, query.EntityName, filters));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var sorts = CheckResult(
            await query.Sorts.ToValidSorts(query.Entity, entitySchemaService.ResolveAttributeVector));
        var kateQuery = CheckResult(query.Entity.OneQuery(res.Filters, sorts, query.Selection.GetLocalAttributes()));
        var item = NotNull(await kateQueryExecutor.One(kateQuery, cancellationToken)).ValOrThrow("Not find record");
        await AttachRelatedEntity(query.Selection,paginationArgs, filterArgs, [item], cancellationToken);
        return item;
    }



    private Record[] SetCursor(Record[] items, Cursor cursor, ValidPagination pagination,
        ImmutableArray<ValidSort>? sorts)
    {
        if (items.Length == 0)
        {
            return [];
        }

        var hasMore = items.Length == pagination.Limit;
        if (hasMore)
        {
            items = cursor.IsForward() 
                ? items.Skip(1).Take(items.Length - 1).ToArray()
                : items.Take(items.Length - 1).ToArray();
        }

        var nextCursor = CheckResult(cursor.GetNextCursor(items, sorts, hasMore));
        if (!string.IsNullOrWhiteSpace(nextCursor.First))
        {
            items.First()[CursorConstants.Cursor] = nextCursor.First;
            items.First()[CursorConstants.HasPreviousPage] = true;
        }

        if (!string.IsNullOrWhiteSpace(nextCursor.Last))
        {
            items.Last()[CursorConstants.Cursor] = nextCursor.Last;
            items.Last()[CursorConstants.HasNextPage] = true;
        }

        return items;
    }

    private async Task AttachRelatedEntity(
        ImmutableArray<GraphAttribute>? attributes,
        Dictionary<string, StringValues> paginationArgs, 
        Dictionary<string, StringValues> filterArgs, 
        Record[] items, 
        CancellationToken cancellationToken)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var attribute in attributes.GetAttributesByType<GraphAttribute>(DisplayType.Lookup))
        {
            await AttachLookup(attribute, paginationArgs,filterArgs, items, cancellationToken);
        }

        foreach (var attribute in attributes.GetAttributesByType<GraphAttribute>(DisplayType.Crosstable))
        {
            await AttachCrosstable(attribute, paginationArgs,filterArgs, items, cancellationToken);
        }
    }

    private async Task AttachCrosstable(GraphAttribute attribute, 
        Dictionary<string, StringValues> paginationArgs, 
        Dictionary<string, StringValues> filterArgs, 
        Record[] items, 
        CancellationToken cancellationToken)
    {
        var cross = NotNull(attribute.Crosstable).ValOrThrow($"not find crosstable of {attribute.GetFullName()}");
        //no need to attach, ignore
        var ids = cross.SourceEntity.PrimaryKeyAttribute.GetUniqValues(items);
        if (ids.Length == 0)
        {
            return;
        }

        var fields = attribute.Selection.GetLocalAttributes();
        var filters = CheckResult(await attribute.Filters.ToValid(cross.TargetEntity, filterArgs,
            entitySchemaService.ResolveAttributeVector));
        var sorts = CheckResult(await attribute.Sorts.ToValidSorts(attribute.Crosstable!.TargetEntity,
            entitySchemaService.ResolveAttributeVector));
        
        var pagination = attribute.ResolvePagination(paginationArgs);
        
        if (pagination is null)
        {
             //get all items and no pagination
             var query = cross.GetRelatedItems(fields, filters, sorts, null, ids);
             var targetRecords = await kateQueryExecutor.Many(query, cancellationToken);
             await AttachRelatedEntity(attribute.Selection, paginationArgs, filterArgs, targetRecords,cancellationToken);
             var targetItemGroups = targetRecords.GroupBy(x => x[cross.SourceAttribute.Field], x => x);
             foreach (var targetGroup in targetItemGroups)
             {
                 var parents = items.Where(local => local[cross.SourceEntity.PrimaryKey].Equals(targetGroup.Key));
                 foreach (var parent in parents)
                 {
                     parent[attribute.Field] = targetGroup.ToArray();
                 }
             }
        }
        else
        {
            foreach (var id in ids)
            {
                var validPagination = pagination.ToValid(cross.TargetEntity.DefaultPageSize);
                validPagination = validPagination with { Limit = validPagination.Limit + 1 };
                
                var query = cross.GetRelatedItems(fields, filters, sorts, validPagination, [id]);
                var targetRecords = await kateQueryExecutor.Many(query, cancellationToken);
                
                 targetRecords = SetCursor(targetRecords, new Cursor(), validPagination, sorts);
                if (targetRecords.Length > 0)
                {
                    await AttachRelatedEntity(attribute.Selection, paginationArgs, filterArgs, targetRecords, cancellationToken);
                }

                foreach (var item in items.Where(x=>x[cross.CrossEntity.PrimaryKey].Equals(id)))
                {
                    item[attribute.Field] = targetRecords;
                }
            }
        }
    }

    private async Task AttachLookup(GraphAttribute attribute, 
        Dictionary<string, StringValues> paginationArgs, 
        Dictionary<string, StringValues> filterArgs, 
        Record[] items, 
        CancellationToken cancellationToken)
    {
        var lookupEntity = NotNull(attribute.Lookup)
            .ValOrThrow($"can not find lookup entity of{attribute.Field}");

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
        var targetRecords = await kateQueryExecutor.Many(query, cancellationToken);
        await AttachRelatedEntity(attribute.Selection, paginationArgs, filterArgs, targetRecords, cancellationToken);

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
}