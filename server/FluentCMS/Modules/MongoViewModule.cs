using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Modules;

public class MongoViewModule(ILogger<MongoViewModule> logger)
{
    
    public static void AddMongoView(WebApplicationBuilder builder, MongoConfig config)
    {
        builder.Services.AddSingleton<INosqlDao>(p => new MongoDao(config, p.GetRequiredService<ILogger<MongoDao>>()));
        builder.Services.AddSingleton<MongoViewModule>();
    }

    public void RegisterMongoViewHook(WebApplication app, string viewName = "*")
    {
        logger.LogInformation($"Registering mongo view hook {viewName}");
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.QueryPreGetList.RegisterDynamic(viewName,
            async (INosqlDao dao, QueryPreGetListArgs p) =>
            {
                var res = InvalidParamExceptionFactory.CheckResult(await dao.Query(p.EntityName, p.Filters,p.Pagination, p.Sorts, p.Cursor));
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetMany.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetManyArgs p) =>
            {
                var res = InvalidParamExceptionFactory.CheckResult(await dao.Query(p.EntityName, p.Filters,p.Pagination));
                return p with { OutRecords = res };
            }
        );
        
        hookRegistry.QueryPreGetOne.RegisterDynamic(
            viewName, 
            async (INosqlDao dao, QueryPreGetOneArgs p) =>
            {
                var records = InvalidParamExceptionFactory.CheckResult(await dao.Query(p.EntityName, p.Filters, new ValidPagination(0,1)));
                return p with { OutRecord = records.First() };
            }
        );
    }
}