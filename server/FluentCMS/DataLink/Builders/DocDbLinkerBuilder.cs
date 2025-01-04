using FluentCMS.DataLink.Types;
using FluentCMS.DataLink.Workers;
using FluentCMS.CoreKit.DocDbQuery;
using FluentCMS.Utils.EventStreaming;

namespace FluentCMS.DataLink.Builders;

public static class DocDbLinkerBuilder
{
    public static IServiceCollection AddNatsMongoLink(
        IServiceCollection services,
        ApiLinks[] apiLinksArray)
    {

        Console.WriteLine(
            $"""
            *********************************************************
            Adding Nats Mongo Link 
            apiLinksArray = {apiLinksArray}
            *********************************************************
            """);
        services.AddSingleton<IStringMessageConsumer, NatsConsumer>();
        services.AddSingleton<IDocumentDbDao, MongoDao>();


        services.AddSingleton(apiLinksArray);
        services.AddHostedService<SyncWorker>();
        services.AddHostedService<MigrateWorker>();
        return services;
    }
}