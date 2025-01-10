
using FormCMS.DataLink.Types;
using FormCMS.DataLink.Builders;

namespace FormCMS;

public static class HostApplicationExt
{
    public static IServiceCollection AddNatsMongoLink(
        this IServiceCollection collection,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddNatsMongoLink(collection, apiLinksArray);
}
