using FluentCMS.Consumers;
using FluentCMS.Utils.DataFetch;
using FluentCMS.Utils.DocumentDb;

namespace FluentCMS.HostAppBuilders;

public static class DocDbLinkerBuilder
{
    public static IServiceCollection AddDocDbLinker(
        IServiceCollection services,
        ApiLinks[] apiLinksArray)
    {
        
        services.AddSingleton<IDocumentDbDao,MongoDao>();
        services.AddSingleton<DataFetcher>();

        services.AddSingleton(apiLinksArray);
        services.AddHostedService<DocDbLinker>(); 
        return services;
    }
}