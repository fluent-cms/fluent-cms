using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.WebAppBuilders;

public record QueryLinks(string Query, string Collection);
public class MongoQueryBuilder(ILogger<MongoQueryBuilder> logger)
{
    public static IServiceCollection AddMongoDbQuery(IServiceCollection services, IEnumerable<QueryLinks> queryCollectionLinks)
    {
        services.AddSingleton(queryCollectionLinks);
        services.AddSingleton<MongoQueryBuilder>();
        services.AddScoped<IDocumentDbDao,MongoDao>();
        return services;
    }

    public WebApplication UserMongoDbQuery(WebApplication app)
    {
        var queryCollectionLinks = app.Services.GetRequiredService<IEnumerable<QueryLinks>>();

        foreach (var (query, collection) in queryCollectionLinks)
        {
            var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
            hookRegistry.QueryPreGetList.RegisterDynamic(
                query,
                async (IDocumentDbDao dao, QueryPreGetListArgs p) =>
                {
                    var res = (await dao.Query(collection, p.Filters, [..p.Sorts], p.Pagination, p.Span)).Ok();
                    return p with { OutRecords = res };
                }
            );

            hookRegistry.QueryPreGetOne.RegisterDynamic(
                query,
                async (IDocumentDbDao dao, QueryPreGetOneArgs p) =>
                {
                    var records = (await dao.Query(collection, p.Filters, [], new ValidPagination(0, 1))).Ok();
                    return p with { OutRecord = records.First() };
                }
            );
        }
        return app;
    }
}