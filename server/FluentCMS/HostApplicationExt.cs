
using FluentCMS.DataLink.Types;
using FluentCMS.DataLink.Builders;

namespace FluentCMS;

public static class HostApplicationExt
{
    public static IServiceCollection AddNatsMongoLink(
        this IServiceCollection collection,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddNatsMongoLink(collection, apiLinksArray);
}
