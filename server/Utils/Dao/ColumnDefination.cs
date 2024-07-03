namespace Utils.Dao;
public record ColumnDefinition
{
    public string ColumnName { get; set; } = "";
    public DatabaseType DataType { get; set; }
}
