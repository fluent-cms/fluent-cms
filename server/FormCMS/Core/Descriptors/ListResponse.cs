namespace FormCMS.Core.Descriptors;

public enum ListResponseMode
{
    Count,
    Items,
    All
}

public record ListResponse(Record[] Items, int TotalRecords);
public record LookupListResponse(bool HasMore, Record[] Items);