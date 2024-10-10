using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public static class SortOrder
{
    public const string Asc = "ASC";
    public const string Desc = "Desc";
}

public sealed record Sort(string FieldName, string Order);

public static class SortHelper
{
    public const string SortKey = "sort";

    public static Result<Sort[]> Parse(Qs.QsDict qsDict)
    {
        var ret = new List<Sort>();

        if (qsDict.Dict.TryGetValue(SortKey, out var fields))
        {
            ret.AddRange(fields.Select(field =>
                new Sort(field.Key, field.Values.FirstOrDefault() == "1" ? SortOrder.Asc : SortOrder.Desc)));
        }
        return ret.ToArray();
    }

    public static Sort[] ReverseOrder(this Sort[] sorts)
    {
        return sorts.Select(sort =>
            sort with { Order = sort.Order == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc }).ToArray();
    }
    
}