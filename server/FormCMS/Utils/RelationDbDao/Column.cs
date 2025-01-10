namespace FormCMS.Utils.RelationDbDao;

public enum ColumnType
{
    Int ,
    Datetime ,

    Text , //slow performance compare to string
    String //has length limit 255 
}


public static class DefaultFields{
    public const string Deleted = "deleted";
}

public record Column(string Name, ColumnType Type);

public static class ColumnHelper
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
