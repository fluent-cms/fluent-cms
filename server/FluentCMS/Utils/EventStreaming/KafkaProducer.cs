using System.Text.Json;
using Confluent.Kafka;

namespace FluentCMS.Utils.EventStreaming;

public sealed class KafkaProducer(ILogger<KafkaProducer> logger,IProducer<string,string> producer):IRecordProducer
{
    public async Task Produce(string topic, RecordMessage message)
    {
        await producer.ProduceAsync(topic,
            new Message<string, string> { Key = message.Key, Value = JsonSerializer.Serialize(message) });
        logger.LogInformation("Produced Message: topic={topic}, entity={entityName}, key={key}", 
            topic, message.EntityName, message.Key);
    }
}