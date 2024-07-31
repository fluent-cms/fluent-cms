using System.Runtime.CompilerServices;

namespace FluentCMS.App;
public class TestEntity
{
    public const string EntityName = "test_entity";
    public const string FieldName = "TestName";
    public const string TestValue = "Alice";
    
    public int id { get; set; }
    public string TestName { get; set; } = "";
}
public class TestService
{
    public string Modify(string id, string change)
    {
        return id + change;
    }
}