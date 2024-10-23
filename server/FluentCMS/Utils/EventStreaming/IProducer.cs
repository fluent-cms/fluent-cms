namespace FluentCMS.Utils.EventStreaming;

public interface IProducer
{
    Task ProduceRecord(string topic, string operation, string entityName, string recordId, Record record);
}
