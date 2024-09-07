using FluentCMS.Services;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.Cache;
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

    public async Task<RecordViewResult> List(string queryName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        CheckResult(view.Filters.ResolveValues(view.Entity!,  schemaService.CastToDatabaseType, querystringDictionary));
        CheckResult(cursor.ResolveBoundaryItem(view.Entity!, schemaService.CastToDatabaseType));
        var pagination = new Pagination { Limit = view.PageSize + 1 }; //get extra record to check if it's the last page
        var (exit, hookResult) = await TriggerHook(Occasion.BeforeQueryList, view, view.Filters, view.Sorts, cursor, pagination);
        if (exit)
        {
            return BuildRecordViewResult(hookResult.Records, cursor, pagination, view.Sorts);
        }

        var attributes = view.Selection.GetLocalAttributes();
        var query = CheckResult(view.Entity!.ListQuery(view.Filters, view.Sorts, pagination, cursor, attributes,
            schemaService.CastToDatabaseType));
        var items = await kateQueryExecutor.Many(query, cancellationToken);
        var results = BuildRecordViewResult(items, cursor, pagination, view.Sorts);
        if (results.Items is not null && results.Items.Length > 0)
        {
            //here use result.Items instead of items, because result.Items omit last record
            await entityService.AttachRelatedEntity(view.Selection, results.Items, cancellationToken);
        }

        return results;
    }

    public async Task<Record[]> Many(string queryName, Pagination? pagination,
        Dictionary<string, StringValues> querystringDictionary,
        CancellationToken cancellationToken)
    {
        var query = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        CheckResult(query.Filters.ResolveValues(query.Entity!, schemaService.CastToDatabaseType,
            querystringDictionary));

        var (exit, hookResult) = await TriggerHook(Occasion.BeforeQueryMany, query, query.Filters);
        if (exit)
        {
            return hookResult.Records;
        }

        var attributes = query.Selection.GetLocalAttributes();
        
        pagination ??= new Pagination { Limit = query.PageSize };
        if (pagination.Limit == 0) pagination.Limit = query.PageSize;

        if (pagination.Offset > query.PageSize * 10)
            throw new InvalidParamException(
                $"invalid offset {pagination.Offset}, maximum value is {query.PageSize * 10}");

        if (pagination.Limit > query.PageSize * 5)
            throw new InvalidParamException(
                $"invalid offset {pagination.Limit}, maximum value is {query.PageSize * 5}");


        //var pagination = new Pagination { Limit = view.PageSize };
        var kateQuery = CheckResult(query.Entity!.ListQuery(query.Filters, query.Sorts, pagination, null, attributes,
            schemaService.CastToDatabaseType));
        var items = await kateQueryExecutor.Many(kateQuery, cancellationToken);
        await entityService.AttachRelatedEntity(query.Selection, items, cancellationToken);
        return items;
    }

    public async Task<Record> One(string queryName, Dictionary<string, StringValues> querystringDictionary,
        CancellationToken cancellationToken)
    {
        var view = await querySchemaService.GetByNameAndCache(queryName, cancellationToken);
        CheckResult(view.Filters.ResolveValues(view.Entity!,  schemaService.CastToDatabaseType, querystringDictionary));
        var (exit, hookResult) = await TriggerHook(Occasion.BeforeQueryOne,view, view.Filters);
        if (exit)
        {
            return hookResult.Record;
        }

        var query = CheckResult(view.Entity!.OneQuery(view.Filters, view.Selection.GetLocalAttributes()));
        var item = NotNull(await kateQueryExecutor.One(query, cancellationToken)).ValOrThrow("Not find record");
        await entityService.AttachRelatedEntity(view.Selection, [item], cancellationToken);
        return item;
    }

    private async Task<(bool, HookReturn)> TriggerHook(Occasion occasion,Query query, Filters filters, Sorts? sorts = null,
        Cursor? cursor = null, Pagination? pagination = null)
    {
        var hookParam = new HookParameter
            { Filters = filters, Sorts = sorts, Cursor = cursor, Pagination = pagination };
        var hookReturn = new HookReturn();
        var exit = await hookRegistry.Trigger(provider, occasion, query, hookParam, hookReturn);
        return (exit, hookReturn);
    }



    private RecordViewResult BuildRecordViewResult(Record[] items, Cursor cursor, Pagination pagination, Sorts? sorts)
    {
        if (items.Length == 0)
        {
            return new RecordViewResult();
        }
        
        var hasMore = items.Length == pagination.Limit;
        if (hasMore)
        {
            items = cursor.First != ""
                ? items.Skip(1).Take(items.Length - 1).ToArray()
                : items.Take(items.Length - 1).ToArray();
        }

        var nextCursor = CheckResult(cursor.GetNextCursor(items, sorts, hasMore));

        return new RecordViewResult
        {
            Items = items,
            First = nextCursor.First,
            HasPrevious = !string.IsNullOrWhiteSpace(nextCursor.First),
            Last = nextCursor.Last,
            HasNext = !string.IsNullOrWhiteSpace(nextCursor.Last)
        };
    }
}