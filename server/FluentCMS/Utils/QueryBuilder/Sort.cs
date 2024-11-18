using System.Collections.Immutable;
using FluentResults;

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
    //sort: id or sort: {id:desc, name:asc}
    public static Result<ImmutableArray<Sort>> ToSort(this IValueProvider valueProvider)
    {
        return valueProvider.Val().Val.Match<Result<ImmutableArray<Sort>>>(
            s => new List<Sort>{new (s,SortOrder.Asc)}.ToImmutableArray(),
            ss =>Result.Fail("Invalid sort input"),
            array => array.Select(x=> new Sort(x.Item1, x.Item2.ToString()!)).ToImmutableArray(),
            errors => Result.Fail(errors)
        );
    }
    public static async Task<Result<ImmutableArray<ValidSort>>> ToValidSorts(
        this IEnumerable<Sort> sorts, 
        LoadedEntity entity,
        IEntityVectorResolver vectorResolver)
    {
        var ret = new List<ValidSort>();
        foreach (var sort in sorts)             
        {
            var (_,_,attr,e) = await vectorResolver.ResolveVector(entity, sort.FieldName);
            if (e is not null)
            {
                return Result.Fail(e);
            }
            ret.Add(new ValidSort(attr,sort.Order));
        }
        return ret.ToImmutableArray();
    }
    
    public static async Task<Result<ImmutableArray<ValidSort>>> Parse(
        LoadedEntity entity, 
        Dictionary<string,QueryStrArgs> dictionary, 
        IEntityVectorResolver vectorResolver)
    {
        var ret = new List<ValidSort>();

        if (dictionary.TryGetValue(SortConstant.SortKey, out var dict))
        {
            foreach (var (fieldName, orderStr) in dict)
            {
                var (_, _, vector, errors) = await vectorResolver.ResolveVector(entity, fieldName);
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