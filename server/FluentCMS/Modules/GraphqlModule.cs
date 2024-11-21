using FluentCMS.Cms.Services;
using FluentCMS.Services;
using FluentCMS.Utils.Graph;
using GraphQL;

namespace FluentCMS.Modules;

public sealed class GraphqlModule( ILogger<GraphqlModule> logger, string path)
{
    public static void AddGraphql(WebApplicationBuilder builder, string path)
    {
        builder.Services.AddSingleton<GraphqlModule>(p => 
            new GraphqlModule(p.GetRequiredService<ILogger<GraphqlModule>>(), path));
        
        builder.Services.AddScoped<Schema>();
        builder.Services.AddScoped<GraphQuery>();
        builder.Services.AddScoped<DateClause>();
        builder.Services.AddScoped<StringClause>();
        builder.Services.AddScoped<IntClause>();
        builder.Services.AddScoped<LogicalOperatorEnum>();
        builder.Services.AddScoped<SortOrderEnum>();
        builder.Services.AddScoped<FilterExpr>();
        builder.Services.AddScoped<SortExpr>();
        
        builder.Services.AddGraphQL(b =>
        {
            b.AddSystemTextJson();
            b.AddUnhandledExceptionHandler(ex =>
            {
                if (ex.Exception is InvalidParamException)
                {
                    ex.ErrorMessage = ex.Exception.Message;
                }
                Console.WriteLine(ex.Exception);
            });
        });
    }

    public void UseGraphqlAsync(WebApplication app)
    {
        logger.LogInformation($"Running graphql, path = ${path}");

        app.UseGraphQL<Schema>();
        app.UseGraphQLGraphiQL(path);
    }
}