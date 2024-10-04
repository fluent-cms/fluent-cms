using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.Modules;

public class EventProduceModule(ILogger<EventProduceModule> logger)
{
    public static void AddKafkaMessageProducer(WebApplicationBuilder builder, string brokerList)
    {
        builder.Services.AddSingleton<EventProduceModule>();
        
        builder.Services.AddSingleton<IProducer>(p =>
            new KafkaProducer(brokerList, p.GetRequiredService<ILogger<KafkaProducer>>()));
    }

    public void RegisterMessageProducerHook(WebApplication app, string entityName = "*")
    {
        logger.LogInformation($"Register message producer hook for {entityName}");
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IProducer>();
        registry.AddHooks(entityName, [Occasion.AfterInsert],
            (EntityMeta meta, Record record) =>
            {
                messageProducer.ProduceRecord(Topics.EntityCreated, Operations.Create, meta, record);
            });

        registry.AddHooks(entityName, [Occasion.AfterUpdate],
            (EntityMeta meta, Record record) =>
            {
                messageProducer.ProduceRecord(Topics.EntityUpdated, Operations.Update, meta, record);
            });
        registry.AddHooks(entityName, [Occasion.AfterDelete],
            (EntityMeta meta, Record record) =>
            {
                messageProducer.ProduceRecord(Topics.EntityDeleted, Operations.Delete, meta, record);
            });
    }
}
    