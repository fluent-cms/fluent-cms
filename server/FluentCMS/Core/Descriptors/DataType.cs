namespace FluentCMS.Core.Descriptors;

public static class DataType
{
    public const string Int = "Int";
    public const string Datetime = "Datetime";

    public const string Text = "Text"; //slow performance compare to string
    public const string String = "String"; //has length limit 255 

    public const string Lookup = "Lookup";
    public const string Junction = "Junction";
    public const string Collection = "Collection";
}

public static class DataTypeHelper
{
    public static readonly HashSet<(string,string)> ValidTypeMap =
    [
        (DataType.Int, DisplayType.Number),
        
        (DataType.Datetime, DisplayType.Datetime),
        (DataType.Datetime, DisplayType.Date),
        
        (DataType.String, DisplayType.Text),
        (DataType.String, DisplayType.Textarea),
        (DataType.String, DisplayType.Image),
        (DataType.String, DisplayType.Gallery),
        (DataType.String, DisplayType.File),
        (DataType.String, DisplayType.Dropdown),
        (DataType.String, DisplayType.Multiselect),
        
        (DataType.Text, DisplayType.Multiselect),
        (DataType.Text, DisplayType.Gallery),
        (DataType.Text, DisplayType.Textarea),
        (DataType.Text, DisplayType.Editor),
        
        (DataType.Lookup, DisplayType.Lookup),
        (DataType.Junction, DisplayType.Picklist),
        (DataType.Collection, DisplayType.EditTable),
    ];
}