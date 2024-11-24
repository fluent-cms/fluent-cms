using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Web;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Query(
    string Name,
    string EntityName,
    string Source,
    string IdeUrl,
    Pagination? Pagination,
    ImmutableArray<Filter> Filters,
    ImmutableArray<Sort> Sorts,
    ImmutableArray<string> ReqVariables
);


public sealed record LoadedQuery(
    string Name,
    string EntityName,
    string Source,
    LoadedEntity Entity,
    Pagination? Pagination,
    ImmutableArray<GraphAttribute> Selection ,
    ImmutableArray<Filter> Filters, // filter need to resolve according to user input
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<string> ReqVariables
);

public static class QueryConstants
{
    public const string LimitKey = "limit";
    public const string OffsetKey = "offset";
    public const string GraphQlRequestSuffix = "GraphQlRequest";
    public const string VariablePrefix = "$";
}

public static class QueryHelper{
    public static LoadedQuery ToLoadedQuery(this Query query, LoadedEntity entity, IEnumerable<GraphAttribute> selection, IEnumerable<ValidSort> sorts)
    {
        return new LoadedQuery(
            Name:query.Name,
            Source: query.Source,
            EntityName:query.EntityName,
            Pagination:query.Pagination,
            ReqVariables:query.ReqVariables,
            Selection:[..selection],
            Sorts:[..sorts],
            Filters:query.Filters,
            Entity:entity
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
        var limit = 0;
        var offset = 0;
        foreach (var input in args)
        {
            var name = input.Name();
            var res = name switch
            {
                QueryConstants.OffsetKey => IntValue(input).BindAction(v => offset = v),
                QueryConstants.LimitKey => IntValue(input).BindAction(v => limit = v),
                SortConstant.SortKey => SortHelper.ParseSorts(input).BindAction(s => sorts.AddRange(s)),
                _ => FilterHelper.ParseFilter(input).BindAction(f => filters.Add(f)),
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        return (sorts.ToArray(), filters.ToArray(), new Pagination(offset, limit));

        Result<int> IntValue(IDataProvider input) => input.TryGetVal(out var val) && val is IntValue intVal
            ? intVal.Value
            : Result.Fail($"Fail to parse int of {input.Name()}");
    }
    
}