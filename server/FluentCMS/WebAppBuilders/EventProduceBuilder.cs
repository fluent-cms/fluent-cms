using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.WebAppBuilders;

public class EventProduceBuilder(ILogger<EventProduceBuilder> logger)
{
    public static IServiceCollection AddKafkaMessageProducer(IServiceCollection services)
    {
        services.AddSingleton<EventProduceBuilder>();
        services.AddSingleton<IRecordProducer, KafkaProducer>();
        return services;
    }

    public WebApplication RegisterMessageProducerHook(WebApplication app, string entityName = "*")
    {
        logger.LogInformation($"Register message producer hook for {entityName}");
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IRecordProducer>();
        registry.EntityPostAdd.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(Topics.EntityCreated,new RecordMessage(Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record));
            return parameter;
        });

        registry.EntityPostUpdate.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(Topics.EntityCreated, new RecordMessage( Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record));
            return parameter;
        });
        registry.EntityPostDel.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(Topics.EntityCreated,new RecordMessage( Operations.Create, parameter.Name,
                parameter.RecordId, parameter.Record));
            return parameter;
        });
        return app;
    }
}
    