using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.WebAppBuilders;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.App;

public static class WebApp
{
    public static async Task<WebApplication>  Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args) ;
        
        builder.AddServiceDefaults();
        builder.AddNatsClient(AppConstants.Nats);
        builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
        
        builder.Services.AddMongoDbQuery()
            .AddNatsMessageProducer()
            .AddPostgresCms(builder.Configuration.GetConnectionString(AppConstants.PostgresCms)!);
        
        var app =builder.Build();
        app.MapDefaultEndpoints();
        
        await app.UseCmsAsync();
        await app.Seed();
        app.RegisterMongoViewHook("post");
        
        return app;
    }

    private static async Task Seed(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
        if (await schemaService.GetByNameDefault("post", SchemaType.Entity) is not null)
        {
            return;
        }

        await AddSchema(scope.ServiceProvider.GetRequiredService<IEntitySchemaService>());
    }

   
    private static async Task AddSchema(IEntitySchemaService entitySchemaService)
    {
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes: [
                    new Attribute(Field: "name", Header:"Name"),
                    new Attribute(Field: "description", Header:"Description"),
                    new Attribute(Field: "image",Header:"Image",Type: DisplayType.Image),
                ],
                DefaultPageSize:50,
                TitleAttribute:"name",
                TableName:"tags",
                Title:"Tag",
                Name: "tag"
            ));
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes: [
                    new Attribute(Field: "name", Header:"Name"),
                    new Attribute(Field: "description", Header:"Description"),
                    new Attribute(Field: "image",Header:"Image",Type: DisplayType.Image),
                ],
                TitleAttribute:"name",
                TableName:"authors",
                DefaultPageSize:50,
                Title:"Author",
                Name: "author"
            ));
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "name", Header:"Name"),
                    new Attribute(Field: "description", Header:"Description"),
                    new Attribute(Field: "image",Header:"Image", Type: DisplayType.Image),
                ],
                TitleAttribute:"name",
                TableName:"categories",
                Title:"Category",
                DefaultPageSize:50,
                Name: "category"
            ));
        await entitySchemaService.SaveTableDefine(
            new Entity(
                Attributes:
                [
                    new Attribute(Field: "title" ,Header:"Title"),
                    new Attribute(Field: "abstract",Header:"Abstract"),
                    new Attribute(Field: "body",Header:"Body"),
                    new Attribute(Field: "image",Header:"Image", Type: DisplayType.Image),
                    
                    new Attribute(Field: "tag",Header:"Tag", DataType: DataType.Na, Type:DisplayType.Junction,Options:"tag"),
                    new Attribute(Field: "author",Header:"Author", DataType: DataType.Na, Type:DisplayType.Junction,Options:"author"),
                    new Attribute(Field: "category",Header:"Category", DataType: DataType.Int, Type:DisplayType.Lookup,Options:"category"),
                ],
                TitleAttribute:"title",
                TableName:"posts",
                Title:"Post",
                DefaultPageSize:50,
                Name: "post"
            ));
    }
}