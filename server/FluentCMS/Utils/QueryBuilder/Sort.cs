using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public static class SortOrder
{
    public const string Asc = "ASC";
    public const string Desc = "Desc";
}

public sealed record Sort(string FieldName, string Order);

public sealed record ValidSort(string FullPath, string Order, ImmutableArray<LoadedAttribute> Attributes)
    : AttributeVector(FullPath: FullPath,Attributes: Attributes);
    

public static class SortHelper
{
    public const string SortKey = "sort";

    public static async Task<Result<ImmutableArray<ValidSort>>> ToValidSorts(this IEnumerable<Sort> sorts, LoadedEntity entity,
        ResolveAttributeDelegate attributeDelegate)
    {
        var ret = new List<ValidSort>();
        foreach (var sort in sorts)
        {
            var attributesRes = await attributeDelegate(entity, sort.FieldName);
            if (attributesRes.IsFailed)
            {
                return Result.Fail(attributesRes.Errors);
            }
            ret.Add(new ValidSort(sort.FieldName,sort.Order,attributesRes.Value));
        }
        return ret.ToImmutableArray();
    }
    
    public static async Task<Result<ImmutableArray<ValidSort>>> Parse(Qs.QsDict qsDict, LoadedEntity entity, ResolveAttributeDelegate attributeDelegate)
    {
        var ret = new List<ValidSort>();

        if (qsDict.Dict.TryGetValue(SortKey, out var pairs))
        {
            foreach (var p in pairs)
            {
                var res = await attributeDelegate(entity, p.Key);
                if (res.IsFailed)
                {
                    return Result.Fail(res.Errors);
                }

                var order = p.Values.FirstOrDefault() == "1" ? SortOrder.Asc : SortOrder.Desc;
                ret.Add(new ValidSort(p.Key, order,res.Value));
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