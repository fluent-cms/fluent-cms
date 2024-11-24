using FluentCMS.Utils.DictionaryExt;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

// Offset, Limit have to bu nullable so they can be resolved from controller
public sealed record Pagination(int? Offset = null, int? Limit = null);

public sealed record ValidPagination(int Offset, int Limit);

public static class PaginationConstants
{
    public const string LimitKey = "limit";
    public const string OffsetKey = "offset";
    public const string PaginationSeparator = ".";
    public const string OffsetSuffix = $"{PaginationSeparator}{OffsetKey}";
    public const string LimitSuffix = $"{PaginationSeparator}{LimitKey}";
}

public static class PaginationHelper
{
    public static bool IsEmpty(this Pagination? pagination)
    {
        return pagination == null || 
               pagination.Offset is null or 0 && pagination.Limit is null or 0;
    }

    public static ValidPagination ToValid(this Pagination? pagination, int defaultPageSize)
    {
        var offset = pagination?.Offset ?? 0;
        var limit = pagination?.Limit is null || pagination.Limit.Value == 0 || pagination.Limit.Value > defaultPageSize
            ? defaultPageSize
            : pagination.Limit.Value;
        return new ValidPagination(offset, limit);
    }

    public static ValidPagination PlusLimitOne(this ValidPagination pagination)
    {
        return pagination with { Limit = pagination.Limit + 1 };
    }

    public static Pagination? ResolvePagination(GraphAttribute attribute,
        Dictionary<string, StringValues> dictionary)
    {
        var key = attribute.Prefix;
        if (!string.IsNullOrWhiteSpace(attribute.Prefix))
        {
            key += PaginationConstants.PaginationSeparator;
        }

        key += attribute.Field;

        var offsetOk = dictionary.TryGetInt(key + PaginationConstants.OffsetSuffix, out var offset);
        var limitOk = dictionary.TryGetInt(key + PaginationConstants.LimitSuffix, out var limit);
        return offsetOk || limitOk ? new Pagination(offset, limit) : null;
    }
}