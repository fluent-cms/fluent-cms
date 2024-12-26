namespace FluentCMS.Utils.RelationDbDao;

public static class ColumnType
{
    public const string Int = "Int";
    public const string Datetime = "Datetime";

    public const string Text = "Text"; //slow performance compare to string
    public const string String = "String"; //has length limit 255 
}


public static class DefaultFields{
    public const string Deleted = "deleted";
}

public record Column(string Name, string Type);

public static class ColumnDefinitionHelper
{
    public static Column[] EnsureDeleted(this Column[] columnDefinitions)
    {
        if (columnDefinitions.FirstOrDefault(x => x.Name == DefaultFields.Deleted) is not null)
        {
            return columnDefinitions;
        }
        return [..columnDefinitions, new Column("deleted", ColumnType.Int)];
    }
}
