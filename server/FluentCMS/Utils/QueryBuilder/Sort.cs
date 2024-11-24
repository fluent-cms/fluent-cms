using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public static class SortOrder
{
    public const string Asc = "Asc";
    public const string Desc = "Desc";
}

public record Sort(string FieldName, string Order);

public sealed record ValidSort(AttributeVector Vector,string Order):Sort(Vector.FullPath, Order);

public static class SortConstant
{
    public const string SortKey = "sort";
    public const string SortExprKey = "sortExpr";
    public const string FieldKey = "field";
    public const string OrderKey = "order";
}

public static class SortHelper
{
    public static Result<Sort[]> ParseSortExpr(IDataProvider provider)
    {
        /*
        if (!provider.Objects(out var objects))
        {
            return Result.Fail($"Failed to get sort expression for {provider.Name()}");
        }
        */
        var sorts = new List<Sort>();
        
        //[{field:"course.teacher.name", sortOrder:Asc}]
        /*
        foreach (var dictionary in objects)
        {
            if (dictionary.TryGetValue(SortConstant.FieldKey, out var field ) && field is string fieldStr &&
                dictionary.TryGetValue(SortConstant.OrderKey, out var order) && order is string orderStr)
            {
                sorts.Add(new Sort(fieldStr,orderStr));
            }
            else
            {
                return Result.Fail("Failed to get sort expression.");
            }
        }
        */
        return sorts.ToArray();
    }
    
    public static Result<Sort[]> ParseSorts( IDataProvider dataProvider)
    {
        if (!dataProvider.TryGetVals(out var array))
        {
            return Result.Fail("Fail to parse sort");
        }
        return array.Select(ToSort).ToArray();

        Sort ToSort(string s)
        {
            //sort: [id, nameDesc]
            return s.EndsWith(SortOrder.Desc)
                ? new Sort(s[..^SortOrder.Desc.Length], SortOrder.Desc)
                : new Sort(s, SortOrder.Asc);
        }
    }
    public static async Task<Result<ValidSort[]>> ToValidSorts(
        this IEnumerable<Sort> list, 
        LoadedEntity entity,
        IEntityVectorResolver vectorResolver)
    {
        var sorts = list.ToArray();
        if (sorts.Length == 0)
        {
            sorts = [new Sort(entity.PrimaryKey, SortOrder.Asc)];
        }

        var ret = new List<ValidSort>();
        foreach (var sort in sorts)             
        {
            var (ok,_,attr,e) = await vectorResolver.ResolveVector(entity, sort.FieldName);
            if (!ok)
            {
                return Result.Fail(e);
            }
            ret.Add(new ValidSort(attr,sort.Order));
        }

        return ret.ToArray();
    }
    
    public static async Task<Result<ValidSort[]>> Parse(
        LoadedEntity entity, 
        Dictionary<string,StrArgs> dictionary, 
        IEntityVectorResolver vectorResolver)
    {
        var ret = new List<ValidSort>();

        if (!dictionary.TryGetValue(SortConstant.SortKey, out var dict)) return ret.ToArray();
        foreach (var (fieldName, orderStr) in dict)
        {
            var (ok, _, vector, errors) = await vectorResolver.ResolveVector(entity, fieldName);
            if (!ok)
            {
                return Result.Fail(errors);
            }
                
            var order = orderStr.ToString() == "1" ? SortOrder.Asc : SortOrder.Desc;
            ret.Add(new ValidSort(vector,order));
        }
        return ret.ToArray();
    }

    public static ImmutableArray<ValidSort> ReverseOrder(this IEnumerable<ValidSort> sorts)
    {
        return [
            ..sorts.Select(sort =>
                sort with { Order = sort.Order == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc })
        ];
    }
    
    public static Result Verify(this IEnumerable<ValidSort> sorts,ImmutableArray<GraphAttribute> attributes, bool allowRecursive)
    {
        foreach (var sort in sorts)
        {
            var find = allowRecursive
                ? attributes.RecursiveFind(sort.FieldName)
                : attributes.FindOneAttr(sort.FieldName);
            if (find is null)
            {
                return Result.Fail($"can not find sort field {sort.FieldName} in selection");
            }
        }

        foreach (var attr in attributes)
        {
            var res = attr.Sorts.Verify(attr.Selection, false);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }
        return Result.Ok();
    }
}