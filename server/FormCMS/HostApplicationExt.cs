
using FormCMS.Cms.Builders;
using FormCMS.DataLink.Types;
using FormCMS.DataLink.Builders;

namespace FormCMS;

public static class HostApplicationExt
{
    public static IServiceCollection AddNatsMongoLink(
        this IServiceCollection collection,
        ApiLinks[] apiLinksArray
    ) => DocDbLinkerBuilder.AddNatsMongoLink(collection, apiLinksArray);
    
    public static IServiceCollection AddPostgresCmsWorker(
        this IServiceCollection services, string connectionString,int delaySeconds = 60, int queryTimeoutSeconds = 120
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Postgres, connectionString, delaySeconds,queryTimeoutSeconds);

    public static IServiceCollection AddSqliteCmsWorker(
        this IServiceCollection services, string connectionString,int delaySeconds = 60, int queryTimeoutSeconds = 120
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.Sqlite,connectionString,delaySeconds,queryTimeoutSeconds);

    public static IServiceCollection AddSqlServerCmsWorker(
        this IServiceCollection services, string connectionString,int delaySeconds = 60, int queryTimeoutSeconds = 120
    ) => CmsWorkerBuilder.AddWorker(services, DatabaseProvider.SqlServer,connectionString,delaySeconds,queryTimeoutSeconds);
}
