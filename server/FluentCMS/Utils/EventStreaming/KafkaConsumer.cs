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
        _consumer.Subscribe(new []{Topics.EntityCreated, Topics.EntityUpdated, Topics.EntityDeleted});
    }

    public async Task<RecordMessage?> Consume(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var consumeResult = _consumer.Consume(cancellationToken);
            return JsonSerializer.Deserialize<RecordMessage>(consumeResult.Message.Value);
        });
    }


    public void Dispose()
    {
        _consumer.Dispose();
    }
}