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
            async (INosqlDao dao, View view, IList<Record> records, Cursor cursor) =>
            {
                var queryResult = await dao.Query(view.Name, view.Filters, view.Sorts, cursor);
                InvalidParamExceptionFactory.CheckResult(queryResult);
                foreach (var dictionary in queryResult.Value)
                {
                    records.Add(dictionary);
                }
                return false;
            });
    }
}