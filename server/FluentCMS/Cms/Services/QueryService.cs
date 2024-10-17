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
    ISchemaService schemaService,
    IQuerySchemaService querySchemaService,
    IEntityService entityService,
    IServiceProvider provider,
    HookRegistry hookRegistry
) : IQueryService
{

    public async Task<QueryResult<Record>> List(string queryName, Cursor cursor, Pagination pagination,
        Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var filters = CheckResult(query.Filters.Resolve(query.EntityName, query.Entity.Attributes,
            schemaService.CastToDatabaseType, querystringDictionary));
        var parsedCursor =CheckResult(cursor.Resolve(query.Entity, schemaService.CastToDatabaseType));
        var validPagination = pagination.ToValid(query.PageSize);
        validPagination = validPagination with { Limit = validPagination.Limit + 1 };// add extra to check has more
        var hookParam = new QueryPreGetListArgs( queryName, query.EntityName, filters,  query.Sorts, parsedCursor, validPagination);
        var res = await hookRegistry.QueryPreGetList.Trigger(provider, hookParam);
        if (res.OutRecords is not null)
        {
            return BuildRecordViewResult(res.OutRecords, cursor, validPagination, query.Sorts);
        }

        var attributes = query.Selection.GetLocalAttributes();
        var kateQuery = CheckResult(query.Entity.ListQuery(filters, query.Sorts, validPagination, parsedCursor, attributes));
        var items = await kateQueryExecutor.Many(kateQuery, cancellationToken);
        
        if (!cursor.IsForward())
        {
            items = items.Reverse().ToArray();
        }

        var results = BuildRecordViewResult(items, cursor, validPagination, query.Sorts);
        if (results.Items is not null && results.Items.Length > 0)
        {
            //here use result.Items instead of items, because result.Items omit last record
            await entityService.AttachRelatedEntity(query.Entity, query.Selection, results.Items, cancellationToken);
        }

        return results;
    }

    public async Task<Record[]> Many(string queryName, 
        Dictionary<string, StringValues> querystringDictionary,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var validPagination = new Pagination().ToValid(query.PageSize);
        var filters = CheckResult(query.Filters.Resolve(query.Entity.Name, query.Selection, schemaService.CastToDatabaseType,
            querystringDictionary));

        var res = await hookRegistry.QueryPreGetMany.Trigger(provider, new QueryPreGetManyArgs(queryName,query.EntityName, filters,validPagination));
        if (res.OutRecords is not null)
        {
            return res.OutRecords;
        }
        
        var attributes = query.Selection.GetLocalAttributes();
        var kateQuery = CheckResult(query.Entity.ListQuery(res.Filters, query.Sorts, validPagination, null, attributes));
        var items = await kateQueryExecutor.Many(kateQuery, cancellationToken);
        await entityService.AttachRelatedEntity(query.Entity,query.Selection, items, cancellationToken);
        return items;
    }

    public async Task<Record> One(string queryName, Dictionary<string, StringValues> querystringDictionary,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        var filters = CheckResult(query.Filters.Resolve(query.Entity.Name, query.Entity.Attributes,  schemaService.CastToDatabaseType, querystringDictionary));
        var res= await hookRegistry.QueryPreGetOne.Trigger(provider, new QueryPreGetOneArgs(queryName, query.EntityName,filters));
        if (res.OutRecord is not null)
        {
            return res.OutRecord;
        }

        var kateQuery = CheckResult(query.Entity.OneQuery(res.Filters, query.Selection.GetLocalAttributes()));
        var item = NotNull(await kateQueryExecutor.One(kateQuery, cancellationToken)).ValOrThrow("Not find record");
        await entityService.AttachRelatedEntity(query.Entity, query.Selection, [item], cancellationToken);
        return item;
    }

    private QueryResult<Record> BuildRecordViewResult(Record[] items, Cursor cursor, ValidPagination pagination, ImmutableArray<Sort>? sorts)
    {
        if (items.Length == 0)
        {
            return new QueryResult<Record>([],"","");
        }
        
        var hasMore = items.Length == pagination.Limit;
        if (hasMore)
        {
            items = cursor.First != ""
                ? items.Skip(1).Take(items.Length - 1).ToArray()
                : items.Take(items.Length - 1).ToArray();
        }

        var nextCursor = CheckResult(cursor.GetNextCursor(items, sorts, hasMore));

        return new QueryResult<Record>(items, nextCursor.First??"", nextCursor.Last??"");
    }
}