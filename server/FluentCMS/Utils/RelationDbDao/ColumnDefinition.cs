namespace FluentCMS.Utils.RelationDbDao;

public static class DataType
{
    public const string Int = "Int";
    public const string Datetime = "Datetime";

    public const string Text = "Text"; //slow performance compare to string
    public const string String = "String"; //has length limit 255 

    public const string Na = "Na";
}

public static class DefaultFields{
    public const string Id = "id";
    public const string Deleted = "deleted";
    public const string CreatedAt = "created_at";
    public const string UpdatedAt = "updated_at";
}

public record ColumnDefinition(string ColumnName, string DataType);

public static class ColumnDefinitionHelper
{
    public static ColumnDefinition[] EnsureDeleted(this ColumnDefinition[] columnDefinitions)
    {
        if (columnDefinitions.FirstOrDefault(x => x.ColumnName == DefaultFields.Deleted) is not null)
        {
            return columnDefinitions;
        }
        return [..columnDefinitions, new ColumnDefinition("deleted", DataType.Int)];
    }
}
