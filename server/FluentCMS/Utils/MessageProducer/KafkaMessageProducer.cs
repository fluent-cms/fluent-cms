using System.Text.Json;
using Confluent.Kafka;
using FluentCMS.Utils.Message;
using FluentCMS.Utils.QueryBuilder;

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
    
    public async Task ProduceRecord(string topic, RecordMeta meta, Record record)
    {
        var message = new RecordMessage { EntityName = meta.EntityName, Id = meta.Id, Data = record };
        await _producer.ProduceAsync(topic,
            new Message<string, string> { Key = message.Key, Value = JsonSerializer.Serialize(message) });
        _logger.LogInformation($"Produced Message: topic={topic}, entity={meta.EntityName}, record id={meta.Id}");
    }
    
    public void Dispose()
    {
        _producer.Dispose();
    }
}