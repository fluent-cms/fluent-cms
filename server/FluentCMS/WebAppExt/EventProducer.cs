using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.WebAppExt;

public static class EventProducer
{
    public static void AddKafkaMessageProducer(this WebApplicationBuilder builder, string brokerList)
    {
        builder.Services.AddSingleton<IProducer>(p =>
            new KafkaProducer(brokerList, p.GetRequiredService<ILogger<KafkaProducer>>()));
    }
    
    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*")
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IProducer>();
        registry.AddHooks(entityName, [Occasion.AfterInsert], (EntityMeta meta,Record record) =>
        {
            messageProducer.ProduceRecord(Topics.EntityCreated, meta, record);
        });
        
        registry.AddHooks(entityName, [Occasion.AfterUpdate], (EntityMeta meta,Record record) =>
        {
            messageProducer.ProduceRecord(Topics.EntityUpdated, meta, record);
        });
        registry.AddHooks(entityName, [Occasion.AfterDelete], (EntityMeta meta,Record record) =>
        {
            messageProducer.ProduceRecord(Topics.EntityDeleted, meta, record);
        });
    } 
}