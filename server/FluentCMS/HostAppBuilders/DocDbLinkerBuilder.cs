using FluentCMS.DataLink.Types;
using FluentCMS.DataLink.Workers;
using FluentCMS.Utils.DocumentDbDao;
using FluentCMS.Utils.EventStreaming;

namespace FluentCMS.HostAppBuilders;

public static class DocDbLinkerBuilder
{
    public static IServiceCollection AddNatsMongoLink(
        IServiceCollection services,
        ApiLinks[] apiLinksArray)
    {
        
        services.AddSingleton<IStringMessageConsumer, NatsConsumer>();
        services.AddSingleton<IDocumentDbDao,MongoDao>();

        services.AddSingleton(apiLinksArray);
        services.AddHostedService<SyncWorker>(); 
        services.AddHostedService<MigrateWorker>(); 
        return services;
    }
}