using SqlKata;

namespace FluentCMS.Utils.Dao;

public record ColumnDefinition
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public object MaxLength { get; set; }
    public string IsNullable { get; set; }
    public object DefaultValue { get; set; }
}


public interface IDao
{
    Task<string> GetPrimaryKeyColumn(string tableName);
    Task<ColumnDefinition[]> GetColumnDefinitions(string tableName);
    Task<dynamic[]> Get(Query query);
    Task<int> Count(Query query);
}