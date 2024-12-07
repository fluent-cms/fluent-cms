using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.Modules;

public class EventProduceModule(ILogger<EventProduceModule> logger)
{
    public static IServiceCollection AddKafkaMessageProducer(IServiceCollection services, string brokerList)
    {
        services.AddSingleton<EventProduceModule>();

        services.AddSingleton<IProducer>(p =>
            new KafkaProducer(brokerList, p.GetRequiredService<ILogger<KafkaProducer>>()));
        return services;
    }

    public WebApplication RegisterMessageProducerHook(WebApplication app, string entityName = "*")
    {
        logger.LogInformation($"Register message producer hook for {entityName}");
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IProducer>();
        registry.EntityPostAdd.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.ProduceRecord(Topics.EntityCreated, Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record);
            return parameter;
        });

        registry.EntityPostUpdate.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.ProduceRecord(Topics.EntityCreated, Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record);
            return parameter;
        });
        registry.EntityPostDel.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.ProduceRecord(Topics.EntityCreated, Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record);
            return parameter;
        });
        return app;
    }
}
    