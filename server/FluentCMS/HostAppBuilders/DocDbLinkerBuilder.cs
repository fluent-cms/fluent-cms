using FluentCMS.Consumers;
using FluentCMS.Utils.DataFetch;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.EventStreaming;

namespace FluentCMS.HostAppBuilders;

public static class DocDbLinkerBuilder
{
    public static IServiceCollection AddDocDbLinker(
        IServiceCollection services,
        KafkaConfig kafkaConfig,
        MongoDaoConfig mongoDaoConfig,
        ApiLinks[] apiLinksArray)
    {
        
        services.AddSingleton(mongoDaoConfig);
        services.AddSingleton<IDocumentDbDao,MongoDao>();

        services.AddSingleton(kafkaConfig);
        services.AddSingleton<IConsumer,KafkaConsumer>();
        
        services.AddSingleton<DataFetcher>();

        services.AddSingleton(apiLinksArray);
        services.AddHostedService<DocDbLinker>(); 
        return services;
    }
}