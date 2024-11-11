using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

// Offset, Limit have to bu nullable so they can be resolved from controller
public sealed record Pagination(int? Offset = null, int? Limit = null);
public sealed record ValidPagination(int Offset, int Limit);

public static class PaginationConstants
{
    public const string OffsetSuffix = ".offset";
    public const string LimitSuffix = ".limit";
    
}
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


    public static ValidPagination PlusLimitOne(this ValidPagination pagination)
    {
        return pagination with { Limit = pagination.Limit + 1 };
    }
    
    
    public static Pagination? ResolvePagination(GraphAttribute attribute,
        Dictionary<string, StringValues> dictionary)
    {
        var key = attribute.Prefix ;
        if (!string.IsNullOrWhiteSpace(attribute.Prefix))
        {
            key += ".";
        }

        key += attribute.Field;
        if (dictionary.TryGetValue(key + PaginationConstants.OffsetSuffix, out var offsetStr) ||
            dictionary.TryGetValue(key + PaginationConstants.LimitSuffix, out var limitStr))
        {
            var offset = GetIntFromDictionary(dictionary, key + PaginationConstants.OffsetSuffix, defaultValue: 0);
            var limit = GetIntFromDictionary(dictionary, key + PaginationConstants.LimitSuffix, defaultValue: 0);
            return new Pagination(offset, limit);
        }

        return null;
    }
    
    private static int GetIntFromDictionary(Dictionary<string, StringValues> dictionary, string key, int defaultValue)
    {
        if (dictionary.TryGetValue(key, out var value) && int.TryParse(value.ToString(), out int result))
        {
            return result;
        }
        return defaultValue;
    }}