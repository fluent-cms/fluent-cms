using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public static class SortOrder
{
    public const string Asc = "ASC";
    public const string Desc = "Desc";
}

public sealed record Sort(string FieldName, string Order);

public sealed record ValidSort(AttributeVector Vector,string Order);

public static class SortConstant
{
    public const string SortKey = "sort";
}

public static class SortHelper
{
    public static async Task<Result<ImmutableArray<ValidSort>>> ToValidSorts(this IEnumerable<Sort> sorts, LoadedEntity entity,
        ResolveVectorDelegate vectorDelegate)
    {
        var ret = new List<ValidSort>();
        foreach (var sort in sorts)
        {
            var res = await vectorDelegate(entity, sort.FieldName);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            ret.Add(new ValidSort(res.Value,sort.Order));
        }
        return ret.ToImmutableArray();
    }
    
    public static async Task<Result<ImmutableArray<ValidSort>>> Parse(Qs.QsDict qsDict, LoadedEntity entity, ResolveVectorDelegate vectorDelegate)
    {
        var ret = new List<ValidSort>();

        if (qsDict.Dict.TryGetValue(SortConstant.SortKey, out var pairs))
        {
            foreach (var p in pairs)
            {
                var (_, _, vector, errors) = await vectorDelegate(entity, p.Key);
                if (errors is not null)
                {
                    return Result.Fail(errors);
                }
                
                var order = p.Values.FirstOrDefault() == "1" ? SortOrder.Asc : SortOrder.Desc;
                ret.Add(new ValidSort(vector,order));
            }
        }
        return ret.ToImmutableArray();
    }

    public static ImmutableArray<ValidSort> ReverseOrder(this IEnumerable<ValidSort> sorts)
    {
        return [
            ..sorts.Select(sort =>
                sort with { Order = sort.Order == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc })
        ];
    }
    
}