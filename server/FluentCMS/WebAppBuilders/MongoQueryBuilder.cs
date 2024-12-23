using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.WebAppBuilders;

public record QueryCollectionLinks(string Query, string Collection);
public class MongoQueryBuilder(ILogger<MongoQueryBuilder> logger, QueryCollectionLinks[] queryLinksArray)
{
    public static IServiceCollection AddMongoDbQuery(IServiceCollection services, IEnumerable<QueryCollectionLinks> queryLinksArray)
    {
        services.AddSingleton(queryLinksArray.ToArray());
        services.AddSingleton<MongoQueryBuilder>();
        services.AddScoped<IDocumentDbDao,MongoDao>();
        return services;
    }

    public WebApplication UseMongoDbQuery(WebApplication app)
    {
        Print();
        RegisterHooks(app);
        return app;
    }

    private void RegisterHooks(WebApplication app)
    {
        foreach (var (query, collection) in queryLinksArray)
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

            hookRegistry.QueryPreGetSingle.RegisterDynamic(
                query,
                async (IDocumentDbDao dao, QueryPreGetSingleArgs p) =>
                {
                    var records = (await dao.Query(collection, p.Filters, [], new ValidPagination(0, 1))).Ok();
                    return p with { OutRecord = records.First() };
                }
            );
        }
    }

    private void Print()
    {
        var info = string.Join(",", queryLinksArray.Select(x => x.ToString()));
        logger.LogInformation(
            """
            *********************************************************
            Using MongoDb Query
            Query Collection Links:
            {queryLinksArray}
            *********************************************************
            """,info); 
    }
}