namespace FluentCMS.Utils.EventStreaming;

public interface IStringMessageProducer
{
    Task Produce(string topic, string msg);
}