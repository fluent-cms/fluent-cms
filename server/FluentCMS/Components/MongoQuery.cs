using FluentCMS.Exceptions;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Components;

public class MongoQuery(ILogger<MongoQuery> logger)
{
    
    public static IServiceCollection AddMongoView(IServiceCollection services, MongoConfig config)
    {
        services.AddSingleton<INosqlDao>(p => new MongoDao(config, p.GetRequiredService<ILogger<MongoDao>>()));
        services.AddSingleton<MongoQuery>();
        return services;
    }

    public WebApplication RegisterMongoViewHook(WebApplication app, string viewName = "*")
    {
        logger.LogInformation($"Registering mongo view hook {viewName}");
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.QueryPreGetList.RegisterDynamic(viewName,
            async (INosqlDao dao, QueryPreGetListArgs p) =>
            {
                var res = InvalidParamExceptionFactory.Ok(await dao.Query(p.EntityName, p.Filters,p.Pagination, p.Sorts, p.Span));
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetMany.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetManyArgs p) =>
            {
                var res = InvalidParamExceptionFactory.Ok(await dao.Query(p.EntityName, p.Filters,p.Pagination,[]));
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetOne.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetOneArgs p) =>
            {
                var records = InvalidParamExceptionFactory.Ok(await dao.Query(p.EntityName, p.Filters, new ValidPagination(0,1),[]));
                return p with { OutRecord = records.First() };
            }
        );
        return app;
    }
}