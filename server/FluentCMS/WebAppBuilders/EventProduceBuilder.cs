using System.Text.Json;
using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.HookFactory;

namespace FluentCMS.WebAppBuilders;

public record EventProduceBuilderOptions(string[] Entities);

public class EventProduceBuilder(ILogger<EventProduceBuilder> logger, EventProduceBuilderOptions options)
{
    private string[] _trackingEntities = [];
    public static IServiceCollection AddNatsMessageProducer(IServiceCollection services, string[] entities)
    {
        services.AddSingleton(new EventProduceBuilderOptions(Entities:entities));
        services.AddSingleton<EventProduceBuilder>();
        services.AddSingleton<IStringMessageProducer, NatsProducer>();
        return services;
    }

    public static IServiceCollection AddKafkaMessageProducer(IServiceCollection services,string[] entities)
    {
        services.AddSingleton(new EventProduceBuilderOptions(Entities:entities));
        services.AddSingleton<EventProduceBuilder>();
        services.AddSingleton<IStringMessageProducer, KafkaProducer>();
        return services;
    }

    public WebApplication UseEventProducer(WebApplication app)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        var messageProducer = app.Services.GetRequiredService<IStringMessageProducer>();
        var option = app.Services.GetRequiredService<EventProduceBuilderOptions>();
        foreach (var entity in option.Entities)
        {
            registry.EntityPostAdd.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    Topics.CmsCrud,
                    EncodeMessage(Operations.Create, parameter.Name, parameter.RecordId, parameter.Record));
                return parameter;
            });

            registry.EntityPostUpdate.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    Topics.CmsCrud,
                    EncodeMessage(Operations.Create, parameter.Name, parameter.RecordId, parameter.Record)
                );
                return parameter;
            });
            registry.EntityPostDel.RegisterAsync(entity, async parameter =>
            {
                await messageProducer.Produce(
                    Topics.CmsCrud,
                    EncodeMessage(Operations.Create, parameter.Name, parameter.RecordId, parameter.Record));
                return parameter;
            });
        }

        return app;
    }

    private static string EncodeMessage(string operation, string entity, string id, Record data
    ) => JsonSerializer.Serialize(new RecordMessage(operation, entity, id, data));
}
    