namespace FluentCMS.Utils.Dao;
public record ColumnDefinition
{
    public string ColumnName { get; set; }
    public DatabaseType DataType { get; set; }
    public object MaxLength { get; set; }
    public string IsNullable { get; set; }
    public object DefaultValue { get; set; }
}
