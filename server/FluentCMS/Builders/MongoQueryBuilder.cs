using FluentCMS.Types;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Builders;

public class MongoQueryBuilder(ILogger<MongoQueryBuilder> logger)
{
    
    public static IServiceCollection AddMongoView(IServiceCollection services, MongoConfig config)
    {
        services.AddSingleton<MongoQueryBuilder>();
        services.AddSingleton<INosqlDao>(p => new MongoDao(config, p.GetRequiredService<ILogger<MongoDao>>()));
        return services;
    }

    public WebApplication RegisterMongoViewHook(WebApplication app, string viewName = "*")
    {
        logger.LogInformation($"Registering mongo view hook {viewName}");
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.QueryPreGetList.RegisterDynamic(viewName,
            async (INosqlDao dao, QueryPreGetListArgs p) =>
            {
                var res = (await dao.Query(p.EntityName, p.Filters,p.Pagination, p.Sorts, p.Span)).Ok();
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetMany.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetManyArgs p) =>
            {
                var res = (await dao.Query(p.EntityName, p.Filters,p.Pagination,[])).Ok();
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetOne.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetOneArgs p) =>
            {
                var records = (await dao.Query(p.EntityName, p.Filters, new ValidPagination(0,1),[])).Ok();
                return p with { OutRecord = records.First() };
            }
        );
        return app;
    }
}