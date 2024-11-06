using System.Collections.Immutable;
using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

public static class SortOrder
{
    public const string Asc = "asc";
    public const string Desc = "desc";
}

public record Sort(string FieldName, string Order);

public sealed record ValidSort(AttributeVector Vector,string Order):Sort(Vector.FullPath, Order);

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
    
    public static async Task<Result<ImmutableArray<ValidSort>>> Parse(
        LoadedEntity entity, 
        Dictionary<string,QueryArgs> dictionary, 
        ResolveVectorDelegate vectorDelegate)
    {
        var ret = new List<ValidSort>();

        if (dictionary.TryGetValue(SortConstant.SortKey, out var dict))
        {
            foreach (var (fieldName, orderStr) in dict)
            {
                var (_, _, vector, errors) = await vectorDelegate(entity, fieldName);
                if (errors?.Count > 0 )
                {
                    return Result.Fail(errors);
                }
                
                var order = orderStr.ToString() == "1" ? SortOrder.Asc : SortOrder.Desc;
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