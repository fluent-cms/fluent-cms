namespace FluentCMS.Utils.HookFactory;

public class HookReturn
{
    public Record Record { get; set; } = new Dictionary<string, object>();
    public Record[] Records { get; set; } = [];
}