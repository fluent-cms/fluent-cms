namespace FluentCMS.Core.Descriptors;

public enum ListResponseMode
{
    count,
    items,
    all
}

public record ListResponse(Record[] Items, int TotalRecords);
public record LookupListResponse(bool HasMore, Record[] Items);