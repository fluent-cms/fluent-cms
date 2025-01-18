using FormCMS.Utils.EnumExt;

namespace FormCMS.Utils.RelationDbDao;

public enum ColumnType
{
    Int ,
    Datetime ,

    Text , //slow performance compare to string
    String //has length limit 255 
}

public enum DefaultColumnNames
{
    Id,
    Deleted,
    CreatedAt,
    UpdatedAt,
    PublicationStatus 
}

public record Column(string Name, ColumnType Type);

public static class ColumnHelper
{
    public static Column[] EnsureDeleted(this Column[] columnDefinitions)
        => columnDefinitions.FirstOrDefault(x => x.Name == DefaultColumnNames.Deleted.ToCamelCase()) is not null
            ? columnDefinitions
            : [..columnDefinitions, new Column(DefaultColumnNames.Deleted.ToCamelCase(), ColumnType.Int)];
}
