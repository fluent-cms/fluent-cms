using System.Text.Json;
using Confluent.Kafka;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Message;

namespace FluentCMS.Utils.MessageProducer;

public sealed class KafkaMessageProducer:IMessageProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessageProducer> _logger;

    public KafkaMessageProducer(string brokerList, ILogger<KafkaMessageProducer> logger)
    {
        var config = new ProducerConfig { BootstrapServers = brokerList };
        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }
    
    public async Task ProduceRecord(string topic, EntityMeta meta, Record record)
    {
        var message = new RecordMessage { EntityName = meta.Entity.Name, Id = meta.Id, Data = record };
        await _producer.ProduceAsync(topic,
            new Message<string, string> { Key = message.Key, Value = JsonSerializer.Serialize(message) });
        _logger.LogInformation($"Produced Message: topic={topic}, entity={meta.Entity.Name}, record id={meta.Id}");
    }
    
    public void Dispose()
    {
        _producer.Dispose();
    }
}