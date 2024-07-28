namespace FluentCMS.Utils.QueryBuilder;

public static class NameFormatter
{
    public static string LowerNoSpace(string s)
    {
        return s.Replace(" ", string.Empty).ToLower();
    }
}