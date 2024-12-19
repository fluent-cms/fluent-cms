using FluentCMS.Cms.Services;
using FluentCMS.WebAppBuilders;

namespace FluentCMS.App;

public static class WebApp
{
    public static async Task<WebApplication>  Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args) ;
        
        builder.AddServiceDefaults();
        builder.AddKafkaProducer<string,string>(AppConstants.Kafka);
        builder.AddMongoDBClient(connectionName: AppConstants.MongoCms);
        
        builder.Services.AddMongoDbQuery()
            .AddKafkaMessageProducer()
            .AddPostgresCms(builder.Configuration.GetConnectionString(AppConstants.PostgresCms)!);
        
        var app =builder.Build();
        app.MapDefaultEndpoints();
        
        await app.UseCmsAsync();
        app.RegisterMongoViewHook("post");
        return app;
    }

    private static async Task SeedDatabase(WebApplication app)
    {
        
    }
}