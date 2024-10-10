namespace FluentCMS.Utils.QueryBuilder;

public sealed record Pagination(int Offset, int Limit);

public static class PaginationHelper{
    public static void Apply(this Pagination pagination,SqlKata.Query? query)
    {
       query?.Offset(pagination.Offset)?.Limit(pagination.Limit);
    }
}