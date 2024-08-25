using FluentCMS.Utils.HookFactory;

namespace FluentCMS.Utils.EventStreaming;

public interface IProducer
{
    Task ProduceRecord(string topic, string operation, EntityMeta meta, Record record);
}
