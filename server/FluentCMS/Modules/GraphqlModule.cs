using FluentCMS.Cms.Services;
using FluentCMS.Utils.Graph;
using GraphQL;

namespace FluentCMS.Modules;

public sealed class GraphqlModule( ILogger<GraphqlModule> logger, string path)
{
    public static void AddGraphql(WebApplicationBuilder builder, string path)
    {
        builder.Services.AddSingleton<GraphqlModule>(p => 
            new GraphqlModule(p.GetRequiredService<ILogger<GraphqlModule>>(), path));
        
        builder.Services.AddScoped<CmsGraphQuery>();
        builder.Services.AddScoped<CmsSchema>();
        builder.Services.AddGraphQL(b => b.AddSystemTextJson());
    }

    public async Task UseGraphqlAsync(WebApplication app)
    {
        logger.LogInformation($"Running graphql, path = ${path}");
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISchemaService>().CacheSchema(SchemaType.Entity);
        app.UseGraphQL<CmsSchema>();
        app.UseGraphQLGraphiQL(path);
    }
}