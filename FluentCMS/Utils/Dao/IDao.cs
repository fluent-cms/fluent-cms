namespace FluentCMS.Utils;

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
}