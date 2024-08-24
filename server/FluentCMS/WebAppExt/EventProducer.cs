using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Message;
using FluentCMS.Utils.MessageProducer;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.WebAppExt;

public static class EventProducer
{
    public static void AddKafkaMessageProducer(this WebApplicationBuilder builder, string brokerList)
    {
        builder.Services.AddSingleton<IMessageProducer>(p =>
            new KafkaMessageProducer(brokerList, p.GetRequiredService<ILogger<KafkaMessageProducer>>()));
    }
    
    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*")
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IMessageProducer>();
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