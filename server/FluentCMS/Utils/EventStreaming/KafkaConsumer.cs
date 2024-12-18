using System.Text.Json;
using Confluent.Kafka;

namespace FluentCMS.Utils.EventStreaming;

public class KafkaConsumer(KafkaConfig config): IConsumer
{
    private readonly IConsumer<string, string> _consumer
        = new ConsumerBuilder<string, string>(
            new ConsumerConfig
            {
                GroupId = config.GroupId,
                BootstrapServers = config.BrokerAddress
            }
        ).Build();

    public void Subscribe() => _consumer.Subscribe(Topics.All);

    public async Task<RecordMessage?> Consume(CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var consumeResult = _consumer.Consume(ct);
            return JsonSerializer.Deserialize<RecordMessage>(consumeResult.Message.Value);
        },ct);
    }


    public void Dispose()
    {
        _consumer.Dispose();
    }
}