namespace FluentCMS.Utils.QueryBuilder;

public class ListResult
{
    public Record[]? Items { get; set; }
    public int TotalRecords { get; set; }
}

public class ViewResult
{
    public Record[]? Items { get; set; }
    public string First { get; set; } = "";
    public bool HasPrevious { get; set; }
    public string Last { get; set; } = "";
    public bool HasNext { get; set; }
} 