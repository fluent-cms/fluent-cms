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
            async (INosqlDao dao, ViewMeta meta, HookReturn hookReturn, Cursor cursor) =>
            {
                hookReturn.Records =InvalidParamExceptionFactory.CheckResult( await dao.Query(meta.View.Entity!.Name, meta.View.Filters, meta.View.Sorts, cursor) );
                return true;
            });
    }
}