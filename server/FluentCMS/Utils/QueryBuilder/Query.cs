using System.Collections.Immutable;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Query(
    string Name,
    string EntityName,
    int PageSize,
    string SelectionSet,
    ImmutableArray<Sort> Sorts,
    ImmutableArray<Filter> Filters);

public sealed record LoadedQuery(
    string Name,
    string EntityName,
    int PageSize,
    ImmutableArray<GraphAttribute> Selection ,
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<Filter> Filters, // filter need to resolve according to user input
    LoadedEntity Entity);

public static class QueryHelper{
    public static LoadedQuery ToLoadedQuery(this Query query, LoadedEntity entity, IEnumerable<GraphAttribute> selection, IEnumerable<ValidSort> sorts)
    {
        return new LoadedQuery(
            query.Name,
            query.EntityName,
            query.PageSize,
            [..selection],
            [..sorts],
            query.Filters,
            entity 
        );
    }
}