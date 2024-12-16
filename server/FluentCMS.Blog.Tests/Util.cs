namespace FluentCMS.Blog.Tests;

public static class Util
{
    public static void SetTestConnectionString()
    {
        const string name = "ConnectionStrings__Sqlite";
        var s = Environment.GetEnvironmentVariable(name);
        var path = Path.Combine(Environment.CurrentDirectory, "_cms_unit_tests.db");
        if (string.IsNullOrWhiteSpace(s))
        {
            Environment.SetEnvironmentVariable(name, $"Data Source={path}");
        }
    }
}