using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Exceptions;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Graph;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL;
using Microsoft.AspNetCore.Rewrite;
using Npgsql;
using Schema = FluentCMS.Graph.Schema;

using static FluentCMS.Exceptions.InvalidParamExceptionFactory;

namespace FluentCMS.Components;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
    AspirePostgres,
}

public sealed class Cms(
    ILogger<Cms> logger, 
    DatabaseProvider databaseProvider, 
    string connectionString,
    string graphPath 
)
{
    private const string FluentCmsContentRoot = "/_content/FluentCMS";
    public string GraphPath => graphPath;

    public static IServiceCollection AddCms(IServiceCollection services ,DatabaseProvider databaseProvider, string connectionString, string graphPath)
    {
        services.AddSingleton<Cms>(p => new Cms(
                p.GetRequiredService<ILogger<Cms>>(),
                databaseProvider,
                connectionString,
                graphPath
            )
        );

        AddRouters();
        Ok(InjectDbServices());
        InjectServices();
        AddGraphql();
        return services;

        void AddGraphql()
        {
            // init for each request, make sure get the latest entity definition
            services.AddScoped<Schema>();
            services.AddScoped<GraphQuery>();
            services.AddScoped<DateClause>();
            services.AddScoped<Clause>();
            services.AddScoped<StringClause>();
            services.AddScoped<IntClause>();
            services.AddScoped<MatchTypeEnum>();
            services.AddScoped<SortOrderEnum>();
            services.AddScoped<FilterExpr>();
            services.AddScoped<SortExpr>();
        
            services.AddGraphQL(b =>
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
        
        void AddRouters()
        {
            services.AddRouting(options => { options.LowercaseUrls = true; });
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        }

        void InjectServices()
        {
            services.AddMemoryCache();
            services.AddSingleton<KeyValueCache<ImmutableArray<Entity>>>(p =>
                new KeyValueCache<ImmutableArray<Entity>>(p, "entities", 60 ));
            
            services.AddSingleton<KeyValueCache<LoadedQuery>>(p =>
                new KeyValueCache<LoadedQuery>(p, "query",60 ));
            
            services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FluentCmsContentRoot}/static-assets/templates/template.html");
                return new PageTemplate(fileInfo.PhysicalPath??"");
            });
            services.AddSingleton<LocalFileStore>(_ => new LocalFileStore(
                           Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"), 1200, 70) ); 
            services.AddSingleton<KateQueryExecutor>(p =>
                new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), 30));
            services.AddSingleton<HookRegistry>();
            
            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();
            
            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageService, PageService>();
            
            services.AddScoped<IProfileService, DummyProfileService>();
        }

        Result InjectDbServices()
        {
            return databaseProvider switch
            {
                DatabaseProvider.Sqlite => InjectSqliteDbServices(),
                DatabaseProvider.Postgres => InjectPostgresDbServices(),
                DatabaseProvider.SqlServer => InjectSqlServerDbServices(),
                DatabaseProvider.AspirePostgres => InjectAspirePostgresDbServices(),
                _ => Result.Fail("unsupported database provider")
            };
        }

        Result InjectSqliteDbServices()
        {
            services.AddSingleton<IKateProvider>(p =>
                new SqliteKateProvider(connectionString, p.GetRequiredService<ILogger<SqliteKateProvider>>()));
            services.AddSingleton<IDefinitionExecutor>(p =>
                new SqliteDefinitionExecutor(connectionString,
                    p.GetRequiredService<ILogger<SqliteDefinitionExecutor>>()));
            return Result.Ok();
        }

        Result InjectSqlServerDbServices()
        {
            services.AddSingleton<IKateProvider>(p =>
                new SqlServerKateProvider(connectionString,
                    p.GetRequiredService<ILogger<SqlServerKateProvider>>()));
            services.AddSingleton<IDefinitionExecutor>(p =>
                new SqlServerDefinitionExecutor(connectionString,
                    p.GetRequiredService<ILogger<SqlServerDefinitionExecutor>>()));
            return Result.Ok();
        }

        Result InjectAspirePostgresDbServices()
        {
            services.AddSingleton<IKateProvider, PostgresKateProvider>();
            services.AddSingleton<IDefinitionExecutor, PostgresDefinitionExecutor>();
            return Result.Ok();
        }

        Result InjectPostgresDbServices()
        {
            var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
            services.AddSingleton<IKateProvider>(p =>
                {
                    var logger = p.GetRequiredService<ILogger<PostgresKateProvider>>();
                    return new PostgresKateProvider(dataSource, logger);
                }
            );
            services.AddSingleton<IDefinitionExecutor>(p =>
                {
                    var logger = p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>();
                    return new PostgresDefinitionExecutor(dataSource, logger);
                }
            );
            return Result.Ok();
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        PrintVersion();
        await InitSchema();
        
        UseApiRouters();
        UseGraphql();
        UseAdminPanel();
        UseSchemaBuilder();
        UseFallbackHomePage();
        UserRedirects();
        
        return;

        void UseSchemaBuilder()
        {
            app.MapStaticAssets();
        }

        void UserRedirects()
        {
            var options = new RewriteOptions();
            options.AddRedirect(@"^admin$", $"{FluentCmsContentRoot}/admin");
            options.AddRedirect(@"^schema$", $"{FluentCmsContentRoot}/schema-ui/list.html");
            app.UseRewriter(options);
        }

        void UseGraphql()
        {
            logger.LogInformation("Running graphql, path = ${graphPath}", graphPath);

            app.UseGraphQL<Schema>();
            app.UseGraphQLGraphiQL(graphPath);
        }
        
        void UseApiRouters()
        {
            app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");
            app.MapControllers();
        }
        
        void UseAdminPanel()
        {
            const string adminPanel = "/admin";
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{FluentCmsContentRoot}{adminPanel}"), subApp =>
            {
                subApp.UseRouting();
                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{adminPanel}", $"{FluentCmsContentRoot}{adminPanel}/index.html");
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{adminPanel}/{{*path:nonfile}}", $"{FluentCmsContentRoot}{adminPanel}/index.html");
                });
            });
        }
        
        void UseFallbackHomePage()
        {
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    if (context.Request.Path == "/")
                    {
                        using var scope = app.Services.CreateScope();
                        var pageService = scope.ServiceProvider.GetRequiredService<IPageService>();
                        var html = $"""
                                    <a href="/admin">Log in to Admin</a><br/>
                                    <a href="/schema">Go to Schema Builder</a>
                                    """;
                        try
                        {
                            html = await pageService.Get(PageConstants.HomePage, new StrArgs());
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e.Message);
                        }

                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync(html);
                    }
                }
            });
        }

        async Task InitSchema()
        {
            using var serviceScope = app.Services.CreateScope();

            var schemaService = serviceScope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable(default);
            await schemaService.EnsureTopMenuBar(default);
        }

       
    }

    private void PrintVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();
        
        logger.LogInformation($"""
                               ***  ******************************************************
                               ***  ******************************************************
                               {title}, Version {informationalVersion?.Split("+").First()}
                               Database : {databaseProvider} - {string.Join(";", parts)}
                               ***  ******************************************************
                               ***  ******************************************************
                               """);
    }
    
}