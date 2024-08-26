using FluentCMS.Services;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

using static InvalidParamExceptionFactory;

public class ViewService(
    IDefinitionExecutor definitionExecutor,
    KateQueryExecutor kateQueryExecutor, 
    ISchemaService schemaService, 
    IEntityService entityService,
    ImmutableCache<View> viewCache,
    IServiceProvider provider,
    HookRegistry hookRegistry
    ) : IViewService
{
    public async Task<RecordViewResult> List(string viewName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await GetView(viewName, cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");

        //get extra record to check if it's the last page
        var pagination = new Pagination
        {
            Limit = view.PageSize + 1
        };

        CheckResult(view.Filters.ResolveValues(view.Entity!, CastToDatabaseType, querystringDictionary));
        var hookParam = new HookParameter
        {
            Filters = view.Filters,
            Sorts = view.Sorts,
            Cursor = cursor,
            Pagination = pagination
        };

        var hookResult = new HookReturn();
        var exits = await hookRegistry.Trigger(provider, Occasion.BeforeQueryView, new ViewMeta(viewName, entity.Name),
            hookParam, hookResult);
        if (exits)
        {
            return BuildRecordViewResult(hookResult.Records, cursor,pagination, view.Sorts);
        }

        var attributes = CheckResult(view.LocalAttributes(InListOrDetail.InList));
        var query = CheckResult(entity.ListQuery(view.Filters, view.Sorts, pagination, cursor,attributes,CastToDatabaseType));
        var items = await kateQueryExecutor.Many(query, cancellationToken);
        var results = BuildRecordViewResult(items, cursor,pagination, view.Sorts);
        if (results.Items is not null && results.Items.Length > 0)
        {
            await AttachRelatedEntity(view, InListOrDetail.InList, results.Items, cancellationToken);
        }

        return results;
    }

    RecordViewResult BuildRecordViewResult(Record[] items, Cursor cursor, Pagination pagination, Sorts? sorts)
    {
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
    
    public async Task<Record[]> Many(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await GetView(viewName, cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");
        
        CheckResult(view.Filters.ResolveValues(view.Entity!, CastToDatabaseType, querystringDictionary));
        var query = CheckResult(entity.ListQuery(  view.Filters,view.Sorts, new Pagination{Limit = view.PageSize}, null,
            CheckResult(view.LocalAttributes(InListOrDetail.InDetail)),CastToDatabaseType));
        var items = await kateQueryExecutor.Many(query,cancellationToken);
        await AttachRelatedEntity(view, InListOrDetail.InDetail, items,cancellationToken);
        return items;
    }

    public async Task<Record> One(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken)
    {
        var view = await GetView(viewName, cancellationToken);
        var entity = NotNull(view.Entity).ValOrThrow($"entity not exist for {viewName}");

        var attributes = CheckResult(view.LocalAttributes(InListOrDetail.InDetail));
        
        CheckResult(view.Filters.ResolveValues(view.Entity!, CastToDatabaseType, querystringDictionary));
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
    
    private async Task<View> GetView(string viewName, CancellationToken cancellationToken)
    {
        var view = await viewCache.GetOrSet(viewName,
            async () => await schemaService.GetViewByName(viewName, cancellationToken));
        return view;
    }
    
    private object CastToDatabaseType(Attribute attribute, string str)
    {
        return definitionExecutor.CastToDatabaseType(attribute.DataType, str);
    }
}