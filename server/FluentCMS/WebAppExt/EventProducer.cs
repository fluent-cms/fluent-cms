using FluentCMS.Utils.MessageProducer;

namespace FluentCMS.WebAppExt;

public static class EventProducer
{
    public static void AddKafkaMessageProducer(this WebApplicationBuilder builder, string brokerList)
    {
        builder.Services.AddSingleton<IMessageProducer>(p =>
            new KafkaMessageProducer(brokerList, p.GetRequiredService<ILogger<KafkaMessageProducer>>()));
        builder.Services.AddSingleton<ProducerHookRegister>();
    }
    
    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*")
    {
        var producerHookRegister = app.Services.GetRequiredService<ProducerHookRegister>();
        producerHookRegister.RegisterMessageProducer(entityName);
    } 
}