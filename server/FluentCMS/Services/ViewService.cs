using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;

using static InvalidParamExceptionFactory;

public class ViewService(
    IDefinitionExecutor definitionExecutor,
    KateQueryExecutor kateQueryExecutor, 
    ISchemaService schemaService, 
    IEntityService entityService,
    KeyValCache<View> viewCache
    ) : IViewService
{
    public async Task<RecordViewResult> List(string viewName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary ,
        CancellationToken cancellationToken
        )
    {
        var view = await ResolvedView(viewName, querystringDictionary,cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");
        if (cursor.Limit == 0 || cursor.Limit > view.PageSize)
        {
            cursor.Limit = view.PageSize;
        }
        cursor.Limit += 1;
        var query = entity.ListQuery(view.Filters, view.Sorts, null, cursor,
            CheckResult(view.LocalAttributes(InListOrDetail.InList)), CastToDatabaseType);
        var items = await kateQueryExecutor.Many(query, cancellationToken);
        
        var hasMore = items.Length == cursor.Limit;
        if (hasMore)
        {
            items = cursor.First != ""
                ? items.Skip(1).Take(items.Length -1).ToArray()
                : items.Take(items.Length -1).ToArray();
        }

        var nextCursor = CheckResult(cursor.GetNextCursor(items, view.Sorts, hasMore));
        await AttachRelatedEntity(view, InListOrDetail.InList, items, cancellationToken);
        
        return new RecordViewResult
        {
            Items = items,
            First = nextCursor.First,
            HasPrevious = !string.IsNullOrWhiteSpace(nextCursor.First),
            Last = nextCursor.Last,
            HasNext = !string.IsNullOrWhiteSpace(nextCursor.Last)
        };
    }


    public async Task<Record[]> Many(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await ResolvedView(viewName, querystringDictionary,cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");
       
        var query = entity.ListQuery(  view.Filters,view.Sorts, new Pagination{Limit = view.PageSize}, null,
            CheckResult(view.LocalAttributes(InListOrDetail.InDetail)),CastToDatabaseType);
        var items = await kateQueryExecutor.Many(query,cancellationToken);
        await AttachRelatedEntity(view, InListOrDetail.InDetail, items,cancellationToken);
        return items;
    }

    public async Task<Record> One(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await ResolvedView(viewName, querystringDictionary,cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");

        var attributes = CheckResult(view.LocalAttributes(InListOrDetail.InDetail));
        var query = CheckResult(entity.OneQuery(view.Filters, attributes ));
        var item = NotNull(await kateQueryExecutor.One(query,cancellationToken))
            .ValOrThrow("Not find record");
        await AttachRelatedEntity(view, InListOrDetail.InDetail, [item],cancellationToken);
        return item;
    }
    
    private async Task AttachRelatedEntity(View view, InListOrDetail scope, Record[] items, CancellationToken cancellationToken)
    {
        foreach (var attribute in CheckResult(view.GetAttributesByType(DisplayType.lookup, scope)))
        {
            await entityService.AttachLookup(attribute, items,cancellationToken,
                entity1 => entity1.LocalAttributes(scope));
        }

        foreach (var attribute in CheckResult(view.GetAttributesByType(DisplayType.crosstable, scope )))
        {
            await entityService.AttachCrosstable(attribute, items,cancellationToken,
                entity1 => entity1.LocalAttributes(scope));
        }
    }
    
    private async Task<View> ResolvedView(string viewName,
        Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await viewCache.GetOrSet(viewName,
            async () => await schemaService.GetViewByName(viewName, cancellationToken));
        CheckResult(view.Filters?.ResolveValues(view.Entity!, CastToDatabaseType, querystringDictionary, null));
        return view;
    }
    private object CastToDatabaseType(Attribute attribute, string str)
    {
        return definitionExecutor.CastToDatabaseType(attribute.DataType, str);
    }
}