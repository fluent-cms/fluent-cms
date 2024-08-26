using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.WebAppExt;

public static class MongoViewExt
{
    public static void AddMongoView(this WebApplicationBuilder builder, MongoConfig config)
    {
        builder.Services.AddSingleton<INosqlDao>(p => new MongoNosqlDao(config));
    }

    public static void RegisterMongoViewHook(this WebApplication app, string entityName = "*")
    {
        var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
        hookRegistry.AddHooks(entityName,[Occasion.BeforeQueryView], 
            async (INosqlDao dao, ViewMeta meta,Filters filters,Sorts sorts, Cursor cursor, HookReturn hookReturn ) =>
            {
                hookReturn.Records =InvalidParamExceptionFactory.CheckResult( await dao.Query(meta.EntityName, filters, sorts, cursor) );
                return true;
            });
    }
}