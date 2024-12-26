namespace FluentCMS.Utils.QueryBuilder;

public static class DataType
{
    public const string Int = "Int";
    public const string Datetime = "Datetime";

    public const string Text = "Text"; //slow performance compare to string
    public const string String = "String"; //has length limit 255 

    public const string Lookup = "Lookup";
    public const string Junction = "Junction";
}