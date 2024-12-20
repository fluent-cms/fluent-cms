using System.Text.Json;
using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.WebAppBuilders;

public class EventProduceBuilder(ILogger<EventProduceBuilder> logger)
{
    public static IServiceCollection AddNatsMessageProducer(IServiceCollection services)
    {
        services.AddSingleton<EventProduceBuilder>();
        services.AddSingleton<IStringMessageProducer, NatsProducer>();
        return services;
    }

    public static IServiceCollection AddKafkaMessageProducer(IServiceCollection services)
    {
        services.AddSingleton<EventProduceBuilder>();
        services.AddSingleton<IStringMessageProducer, KafkaProducer>();
        return services;
    }

    public WebApplication RegisterMessageProducerHook(WebApplication app, string entityName = "*")
    {
        logger.LogInformation("Register message producer hook for {entityName}",entityName);
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IStringMessageProducer>();
        registry.EntityPostAdd.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(
                Topics.CmsCrud, 
                EncodeMessage(Operations.Create, parameter.Name, parameter.RecordId, parameter.Record));
            return parameter;
        });

        registry.EntityPostUpdate.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(
                Topics.CmsCrud,
                EncodeMessage(Operations.Create, parameter.Name, parameter.RecordId, parameter.Record)
            );
            return parameter;
        });
        registry.EntityPostDel.RegisterAsync(entityName, async parameter =>
        {
            await messageProducer.Produce(
                Topics.CmsCrud,
                EncodeMessage( Operations.Create, parameter.Name, parameter.RecordId, parameter.Record));
            return parameter;
        });
        return app;
    }

    private static string EncodeMessage(string operation, string entity, string id, Record data
    ) => JsonSerializer.Serialize<RecordMessage>(new RecordMessage(operation, entity, id, data));
}
    