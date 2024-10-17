namespace FluentCMS.Utils.QueryBuilder;

// Offset, Limit have to bu nullable so they can be resolved from controller
public sealed record Pagination(int? Offset = null, int? Limit = null);
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