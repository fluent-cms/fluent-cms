using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.MessageProducer;

public interface IMessageProducer
{
    Task ProduceRecord(string topic, RecordMeta meta, Record record);
}
