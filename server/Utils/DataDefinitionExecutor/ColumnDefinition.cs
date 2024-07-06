namespace Utils.DataDefinitionExecutor;

public enum DataType
{
    Int,
    Datetime,

    Text, //slow performance compare to string
    String, //has length limit 255 

    Na,
}

public record ColumnDefinition
{

    public string ColumnName { get; set; } = "";
    public DataType DataType { get; set; }
}
