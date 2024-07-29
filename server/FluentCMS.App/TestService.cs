namespace FluentCMS.App;

public class TestService
{
    public IDictionary<string,object> HandleRecord(IDictionary<string,object> record)
    {
        Console.WriteLine(record);
        return record;
    }
}