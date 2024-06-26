namespace FluentCMS.Services;

public struct ListResult
{
    public IDictionary<string, object>[]? Items { get; set; }
    public int TotalRecords { get; set; }
}

public struct ViewResult
{
    public IDictionary<string, object>[]? Items { get; set; }
    public string First { get; set; }
    public string Last { get; set; }
} 