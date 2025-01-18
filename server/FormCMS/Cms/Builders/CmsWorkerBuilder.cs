using FormCMS.Cms.Workers;
using FormCMS.Utils.RelationDbDao;

namespace FormCMS.Cms.Builders;

public static class CmsWorkerBuilder
{
    public static IServiceCollection AddWorker(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        int delaySeconds
        )
    {
        Console.WriteLine(
            $"""
             *********************************************************
             Adding CMS Publishing Worker
             *********************************************************
             """);

        services.AddDao(databaseProvider, connectionString );
        
        services.AddSingleton(new KateQueryExecutorOption(300));
        services.AddScoped<KateQueryExecutor>();
        services.AddSingleton(new DataPublishingWorkerOptions(delaySeconds));
        services.AddHostedService<DataPublishingWorker>();
        return services;
    }
    
}