using System.Text.Json;
using Confluent.Kafka;

namespace FluentCMS.Utils.EventStreaming;

public class KafkaConsumer(IConsumer<string,string> consumer): IRecordConsumer
{
    public void Subscribe() => consumer.Subscribe(Topics.All);

    public RecordMessage? Consume(CancellationToken ct)
        => JsonSerializer.Deserialize<RecordMessage>(consumer.Consume(ct).Message.Value);

}