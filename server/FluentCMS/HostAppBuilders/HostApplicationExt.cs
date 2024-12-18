using FluentCMS.Consumers;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.EventStreaming;

namespace FluentCMS.HostAppBuilders;


public static class HostApplicationExt
{
    public static IServiceCollection AddMongoCmsConsumer(
        this HostApplicationBuilder builder,
        KafkaConfig kafkaConfig,
        MongoDaoConfig dbConfig,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddDocDbLinker(builder.Services, kafkaConfig, dbConfig, apiLinksArray);
}
