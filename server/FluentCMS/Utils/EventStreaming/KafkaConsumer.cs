using System.Text.Json;
using Confluent.Kafka;

namespace FluentCMS.Utils.EventStreaming;

public class KafkaConsumer(KafkaConfig config): IConsumer
{
    private readonly IConsumer<string, string> _consumer =
        new ConsumerBuilder<string, string>(new ConsumerConfig
            { GroupId = config.GroupId, BootstrapServers = config.BrokerAddress }).Build();

    public void Subscribe()
    {
        _consumer.Subscribe(Topics.EntityUpdated);
        _consumer.Subscribe(Topics.EntityCreated);
        _consumer.Subscribe(Topics.EntityDeleted);
    }

    public (string,RecordMessage?) Consume(CancellationToken cancellationToken)
    {
        var consumeResult = _consumer.Consume(cancellationToken);
        var msg = JsonSerializer.Deserialize<RecordMessage>(consumeResult.Message.Value);
        return (consumeResult.Topic, msg);
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}