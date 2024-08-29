using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.WebAppExt;

public static class MongoViewExt
{
    public static void AddMongoView(this WebApplicationBuilder builder, MongoConfig config)
    {
        builder.Services.AddSingleton<INosqlDao>(p => new MongoDao(config, p.GetRequiredService<ILogger<MongoDao>>()));
    }

    public static void RegisterMongoViewHook(this WebApplication app, string viewName = "*")
    {
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.AddHooks(
            viewName, 
            [Occasion.BeforeQueryList],
            async (INosqlDao dao, QueryMeta meta, 
                Filters filters, Sorts sorts, Cursor cursor, Pagination pagination,
                HookReturn hookReturn) =>
            {
                var res = await dao.Query(meta.EntityName, filters, sorts, cursor, pagination);
                hookReturn.Records = InvalidParamExceptionFactory.CheckResult(res);
                return true;
            }
        );
        
        hookRegistry.AddHooks(
            viewName, 
            [Occasion.BeforeQueryMany],
            async (INosqlDao dao, QueryMeta meta, Filters filters, HookReturn hookReturn) =>
            {
                hookReturn.Records =
                    InvalidParamExceptionFactory.CheckResult(await dao.Query(meta.EntityName, filters));
                return true;
            }
        );
        
        hookRegistry.AddHooks(
            viewName, 
            [Occasion.BeforeQueryOne],
            async (INosqlDao dao, QueryMeta meta, Filters filters,HookReturn hookReturn) =>
            {
                var records = InvalidParamExceptionFactory.CheckResult(await dao.Query(meta.EntityName, filters));
                if (records.Length > 0)
                {
                    hookReturn.Record = records[0];
                }
                return true;
            }
        );
    }
}