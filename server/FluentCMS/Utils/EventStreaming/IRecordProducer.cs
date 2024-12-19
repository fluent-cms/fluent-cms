namespace FluentCMS.Utils.EventStreaming;

public interface IRecordProducer
{
    Task Produce(string topic, RecordMessage msg);
}
