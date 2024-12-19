using FluentCMS.Consumers;

namespace FluentCMS.HostAppBuilders;


public static class HostApplicationExt
{
    public static IServiceCollection AddMongoLinker(
        this IServiceCollection collection,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddDocDbLinker(collection, apiLinksArray);
}
