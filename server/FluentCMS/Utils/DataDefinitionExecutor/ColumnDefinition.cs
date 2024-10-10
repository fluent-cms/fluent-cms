namespace FluentCMS.Utils.DataDefinitionExecutor;

public static class DataType
{
    public const string Int = "int";
    public const string Datetime = "datetime";

    public const string Text = "text"; //slow performance compare to string
    public const string String = "string"; //has length limit 255 

    public const string Na = "na";
}

public record ColumnDefinition(string ColumnName, string DataType);

public static class ColumnDefinitionHelper
{
    public static ColumnDefinition[] EnsureDeleted(this ColumnDefinition[] columnDefinitions)
    {
        if (columnDefinitions.FirstOrDefault(x => x.ColumnName == "delete") is not null)
        {
            return columnDefinitions;
        }
        return [..columnDefinitions, new ColumnDefinition("deleted", DataType.Int)];
    }
}
