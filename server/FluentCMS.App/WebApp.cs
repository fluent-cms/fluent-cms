using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.Test;
using FluentCMS.WebAppBuilders;

namespace FluentCMS.App;

public static class WebApp
{
    private const string Cors = "cors";

    public static async Task<WebApplication?> Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (builder.Configuration.GetValue<bool>(AppConstants.EnableWebApp) is not true)
        {
            return null;
        }
        if (builder.Environment.IsDevelopment()) builder.Services.AddCorsPolicy();
        builder.AddServiceDefaults();

        builder.AddNatsClient(AppConstants.Nats);
        var entities = builder.Configuration.GetRequiredSection("TrackingEntities").Get<string[]>()!;
        builder.Services.AddNatsMessageProducer(entities);
        
        
        builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
        var queryLinksArray = builder.Configuration.GetRequiredSection("QueryLinksArray").Get<QueryCollectionLinks[]>()!;
        builder.Services.AddMongoDbQuery(queryLinksArray);

        builder.Services.AddPostgresCms(builder.Configuration.GetConnectionString(AppConstants.PostgresCms)!);


        var app = builder.Build();
        app.UseCors(Cors);
        app.MapDefaultEndpoints();
        await app.UseCmsAsync();

        if (builder.Configuration.GetValue<bool>("add-schema")) await app.AddSchema();
        if (builder.Configuration.GetValue<bool>("add-data")) await app.AddData();
        if (builder.Configuration.GetValue<bool>("add-query")) await app.AddQuery();

        return app;
    }

    private static void AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                Cors,
                policy =>
                {
                    policy.WithOrigins("http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }

    private static async Task AddQuery(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IQuerySchemaService>();
        var query = new Query
        (
            Name: "post_sync",
            EntityName: "post",
            Filters: [new Filter("id", MatchTypes.MatchAll, [new Constraint(Matches.In, ["$id"])])],
            Sorts: [new Sort("id", SortOrder.Asc)],
            ReqVariables: [],
            Source:
            """
            query post_sync($id:Int){
              postList(idSet:[$id],sort:id){
                id, title, body,abstract
                tag{id,name}
                category{id,name}
                author{id,name}
              }
            }
            """
        );
        await service.SaveQuery(query);
    }

    private static async Task AddData(IEntityService service, string tableName, string[] fields, int commitCount)
    {

        for (var i = 0; i < commitCount; i++)
        {
            var vals = new List<IEnumerable<object>>();
            for (var j = 0; j < 1000; j++)
            {
                var idx = i % 1000 + j + 1;
                var val = fields.Select(s => $"{s}-{idx}").Cast<object>().ToArray();
                vals.Add(val);
            }

            await service.BatchInsert(tableName, fields, vals);
        }
    }

    private static async Task AddData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEntityService>();
        await AddData(service, "tags", ["name", "description", "image"], 1000);
        await AddData(service, "authors", ["name", "description", "image"], 1000);
        await AddData(service, "categories", ["name", "description", "image"], 1000);

        for (var i = 0; i < 1000; i++)
        {
            var vals = new List<IEnumerable<object>>();
            for (var j = 0; j < 1000; j++)
            {
                var idx = i % 1000 + j + 1;
                object[] val =
                    [$"title-{idx}", $"abstract-{idx}", $"body-{idx}", $"image-{idx}", idx];
                vals.Add(val);
            }

            await service.BatchInsert("posts", ["title", "abstract", "body", "image", "category"], vals);
        }

        for (var i = 0; i < 1000; i++)
        {
            var vals = new List<IEnumerable<object>>();
            for (var j = 0; j < 1000; j++)
            {
                var id = i * 1000 + j + 1;
                vals.Add([id, id]);
            }

            await service.BatchInsert("author_post", ["post_id", "author_id"], vals);
            await service.BatchInsert("post_tag", ["post_id", "tag_id"], vals);
        }
    }

    private static async Task AddSchema(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var entitySchemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
        foreach (var entity in BlogsTestData.Entities)
        {
            await entitySchemaService.SaveTableDefine(entity);
        }
    }
}