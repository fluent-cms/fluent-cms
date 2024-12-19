using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.WebAppBuilders;

public class MongoQueryBuilder(ILogger<MongoQueryBuilder> logger)
{
    
    public static IServiceCollection AddMongoDbQuery(IServiceCollection services)
    {
        services.AddSingleton<MongoQueryBuilder>();
        services.AddScoped<IDocumentDbDao,MongoDao>();
        return services;
    }

    public WebApplication RegisterMongoViewHook(WebApplication app, string viewName = "*")
    {
        logger.LogInformation($"Registering mongo view hook {viewName}");
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.QueryPreGetList.RegisterDynamic(
            viewName,
            async (IDocumentDbDao dao, QueryPreGetListArgs p) =>
            {
                var res = (await dao.Query(p.EntityName, p.Filters, [..p.Sorts], p.Pagination, p.Span)).Ok();
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetOne.RegisterDynamic(
            viewName, 
            async (IDocumentDbDao dao, QueryPreGetOneArgs p) =>
            {
                var records = (await dao.Query(p.EntityName, p.Filters,[], new ValidPagination(0,1))).Ok();
                return p with { OutRecord = records.First() };
            }
        );
        return app;
    }
}