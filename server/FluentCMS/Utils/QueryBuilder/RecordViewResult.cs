namespace FluentCMS.Utils.QueryBuilder;

public class RecordQueryResult : QueryResult<Record>;

public class QueryResult<T>
{
    public T[]? Items { get; set; }
    public string First { get; set; } = "";
    public string Last { get; set; } = "";
} 