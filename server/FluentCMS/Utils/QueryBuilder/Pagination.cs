namespace FluentCMS.Utils.QueryBuilder;

public sealed record Pagination(int? Offset, int? Limit);
public sealed record ValidPagination(int Offset, int Limit);

public static class PaginationHelper
{
    public static ValidPagination ToValid(this Pagination pagination, int defaultPageSize)
    {
        var offset = pagination.Offset ?? 0;
        var limit = pagination.Limit is null || pagination.Limit.Value == 0 || pagination.Limit.Value > defaultPageSize 
            ? defaultPageSize
            : pagination.Limit.Value;
        return new ValidPagination(offset, limit);
    }
}