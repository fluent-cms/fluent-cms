using FluentCMS.Cms.Services;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.WebAppBuilders;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

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

    private static async Task AddData(IEntityService service, string entity, string[] fields, int commitCount)
    {

        for (var i = 0; i < commitCount; i++)
        {
            var vals = new List<IEnumerable<object>>();
            for (var j = 0; j < 1000; j++)
            {
                var val = fields.Select(s => $"{entity}.{s}-{i}-{j}").Cast<object>().ToArray();
                vals.Add(val);
            }

            await service.BatchInsert(entity, fields, vals);
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
                object[] val =
                    [$"title-{i}-{j}", $"abstrct-{i}-{j}", $"body-{i}-{j}", $"imge-{i}-{j}", i * 1000 + j + 1];
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
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "name", Header: "Name"),
                    new Attribute(Field: "description", Header: "Description"),
                    new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                ],
                DefaultPageSize: 50,
                TitleAttribute: "name",
                TableName: "tags",
                Title: "Tag",
                Name: "tag"
            ));


        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "name", Header: "Name"),
                    new Attribute(Field: "description", Header: "Description"),
                    new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                ],
                TitleAttribute: "name",
                TableName: "authors",
                DefaultPageSize: 50,
                Title: "Author",
                Name: "author"
            ));
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "name", Header: "Name"),
                    new Attribute(Field: "description", Header: "Description"),
                    new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),
                ],
                TitleAttribute: "name",
                TableName: "categories",
                Title: "Category",
                DefaultPageSize: 50,
                Name: "category"
            ));
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "title", Header: "Title"),
                    new Attribute(Field: "abstract", Header: "Abstract"),
                    new Attribute(Field: "body", Header: "Body"),
                    new Attribute(Field: "image", Header: "Image", DisplayType: DisplayType.Image),

                    new Attribute(Field: "tag", Header: "Tag", DataType: DataType.Junction, DisplayType: DisplayType.Junction,
                        Options: "tag"),
                    new Attribute(Field: "author", Header: "Author", DataType: DataType.Junction, DisplayType: DisplayType.Junction,
                        Options: "author"),
                    new Attribute(Field: "category", Header: "Category", DataType: DataType.Lookup,
                        DisplayType: DisplayType.Lookup, Options: "category"),
                ],
                TitleAttribute: "title",
                TableName: "posts",
                Title: "Post",
                DefaultPageSize: 50,
                Name: "post"
            ));
    }
}