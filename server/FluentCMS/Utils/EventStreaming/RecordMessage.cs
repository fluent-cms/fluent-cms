namespace FluentCMS.Utils.EventStreaming;

public class RecordMessage
{
    public string EntityName { get; set; } = "";
    public string Id { get; set; } = "";
    public Record Data { get; set; } = null !;
    
    public string Key => $"{EntityName}_{Id}";
}