using System.Collections.Immutable;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Query(
    string Name,
    string EntityName,
    string Source,
    ImmutableArray<Filter> Filters,
    ImmutableArray<Sort> Sorts,
    ImmutableArray<string> ReqVariables,
    string IdeUrl = "",
    Pagination? Pagination= default
);


public sealed record LoadedQuery(
    string Name,
    string EntityName,
    string Source,
    LoadedEntity Entity,
    Pagination? Pagination,
    ImmutableArray<GraphAttribute> Selection ,
    ImmutableArray<ValidFilter> Filters, 
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<string> ReqVariables
);

public static class QueryConstants
{
    public const string VariablePrefix = "$";
}

public static class QueryHelper{
    public static LoadedQuery ToLoadedQuery(this Query query, 
        LoadedEntity entity, 
        IEnumerable<GraphAttribute> selection, 
        IEnumerable<ValidSort> sorts,
        IEnumerable<ValidFilter> filters
        )
    {
        return new LoadedQuery(
            Name:query.Name,
            Source: query.Source,
            EntityName:query.EntityName,
            Pagination:query.Pagination,
            ReqVariables:query.ReqVariables,
            Entity:entity,
            Selection:[..selection],
            Sorts:[..sorts],
            Filters:[..filters]
        );
    }

    public static Result VerifyVariable(this LoadedQuery query, StrArgs args)
    {
        foreach (var key in query.ReqVariables)
        {
            if (!args.ContainsKey(key))
            {
                return Result.Fail($"Variable {key} doesn't exist");
            }
        }

        return Result.Ok();
    }
    
public static Result<(Sort[], Filter[], Pagination)> ParseArguments(IDataProvider[] args)
    {
        HashSet<string> keys = [FilterConstants.FilterExprKey, SortConstant.SortExprKey];
        var simpleArgs = args.Where(x => !keys.Contains(x.Name()));

        if (!ParseSimpleArguments(simpleArgs).Try(out var simpleRes, out var err))
        {
            return Result.Fail(err);
        }
        
        var (sorts,filters,pagination) = simpleRes;
        foreach (var input in args.Where(x => keys.Contains(x.Name())))
        {
            var res = input.Name() switch
            {
                FilterConstants.FilterExprKey => FilterHelper.ParseFilterExpr(input)
                    .BindAction(f => filters = [..filters, ..f]),
                SortConstant.SortExprKey => SortHelper.ParseSortExpr(input).BindAction(s => sorts = [..sorts, ..s]),
                _ => Result.Ok()
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }        
        return (sorts, filters, pagination);
    }
    
    public static Result<(Sort[], Filter[], Pagination)> ParseSimpleArguments(IEnumerable<IDataProvider> args)
    {
        var sorts = new List<Sort>();
        var filters = new List<Filter>();
        string? limit = null;
        string? offset = null;
        foreach (var input in args)
        {
            var name = input.Name();
            var res = name switch
            {
                PaginationConstants.OffsetKey => Val(input).BindAction(v => offset = v),
                PaginationConstants.LimitKey => Val(input).BindAction(v => limit = v),
                SortConstant.SortKey => SortHelper.ParseSorts(input).BindAction(s => sorts.AddRange(s)),
                _ => FilterHelper.ParseFilter(input).BindAction(f => filters.Add(f)),
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return (sorts.ToArray(), filters.ToArray(), new Pagination(offset, limit));

        Result<string> Val(IDataProvider input) => input.TryGetVal(out var val) && !string.IsNullOrWhiteSpace(val) 
            ? val
            : Result.Fail($"Fail to parse value of {input.Name()}");
    }
    
}