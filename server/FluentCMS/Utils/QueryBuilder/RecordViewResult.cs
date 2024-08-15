namespace FluentCMS.Utils.QueryBuilder;

public class RecordViewResult : ViewResult<Record>;

public class ViewResult<T>
{
    public T[]? Items { get; set; }
    public string First { get; set; } = "";
    public bool HasPrevious { get; set; }
    public string Last { get; set; } = "";
    public bool HasNext { get; set; }
} 