namespace FluentCMS.Services;

public struct ListResult
{
    public Record[]? Items { get; set; }
    public int TotalRecords { get; set; }
}

public struct ViewResult
{
    public Record[]? Items { get; set; }
    public string First { get; set; }
    public bool HasPrevious { get; set; }
    public string Last { get; set; }
    public bool HasNext { get; set; }
} 