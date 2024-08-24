using FluentCMS.Utils.HookFactory;

namespace FluentCMS.Utils.MessageProducer;

public interface IMessageProducer
{
    Task ProduceRecord(string topic, EntityMeta meta, Record record);
}
