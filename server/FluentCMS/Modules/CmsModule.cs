using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Modules;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public sealed class CmsModule(
    ILogger<CmsModule> logger, 
    DatabaseProvider databaseProvider, 
    string connectionString 
)
{
    private const string FluentCmsContentRoot = "/_content/FluentCMS";

    public  HookRegistry GetHookRegistry(WebApplication app) => app.Services.GetRequiredService<HookRegistry>();
    public static void AddCms(WebApplicationBuilder builder,DatabaseProvider databaseProvider, string connectionString)
    {
        builder.Services.AddSingleton<CmsModule>(p => new CmsModule(
                p.GetRequiredService<ILogger<CmsModule>>(),
                databaseProvider,
                connectionString
            )
        );

        AddRouters();
        InjectDbServices();
        InjectServices();
        return;

        void AddRouters()
        {
            builder.Services.AddRouting(options => { options.LowercaseUrls = true; });
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        }

        void InjectServices()
        {
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<NonExpiringKeyValueCache<ImmutableArray<Entity>>>(p =>
                new NonExpiringKeyValueCache<ImmutableArray<Entity>>(p.GetRequiredService<IMemoryCache>(), "entities"));
            
            builder.Services.AddSingleton<ExpiringKeyValueCache<LoadedQuery>>(p =>
                new ExpiringKeyValueCache<LoadedQuery>(p.GetRequiredService<IMemoryCache>(), 30, "query"));
            
            builder.Services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FluentCmsContentRoot}/static-assets/templates/template.html");
                return new PageTemplate(fileInfo.PhysicalPath??"");
            });
            builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore(
                           Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"), 1200, 70) ); 
            builder.Services.AddSingleton<KateQueryExecutor>(p =>
                new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), 30));
            builder.Services.AddSingleton<HookRegistry>();
            
            builder.Services.AddScoped<ISchemaService, SchemaService>();
            builder.Services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            builder.Services.AddScoped<IQuerySchemaService, QuerySchemaService>();
            
            builder.Services.AddScoped<IEntityService, EntityService>();
            builder.Services.AddScoped<IQueryService, QueryService>();
            builder.Services.AddScoped<IPageService, PageService>();
            
            builder.Services.AddScoped<IProfileService, DummyProfileService>();
        }

        void InjectDbServices()
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.Sqlite:
                    builder.Services.AddSingleton<IKateProvider>(p =>
                        new SqliteKateProvider(connectionString, p.GetRequiredService<ILogger<SqliteKateProvider>>()));
                    builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                        new SqliteDefinitionExecutor(connectionString,
                            p.GetRequiredService<ILogger<SqliteDefinitionExecutor>>()));

                    break;
                case DatabaseProvider.Postgres:
                    builder.Services.AddSingleton<IKateProvider>(p =>
                        new PostgresKateProvider(connectionString,
                            p.GetRequiredService<ILogger<PostgresKateProvider>>()));
                    builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                        new PostgresDefinitionExecutor(connectionString,
                            p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>()));
                    break;
                case DatabaseProvider.SqlServer:
                    builder.Services.AddSingleton<IKateProvider>(p =>
                        new SqlServerKateProvider(connectionString,
                            p.GetRequiredService<ILogger<SqlServerKateProvider>>()));
                    builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                        new SqlServerDefinitionExecutor(connectionString,
                            p.GetRequiredService<ILogger<SqlServerDefinitionExecutor>>()));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseProvider), databaseProvider, null);
            }
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        PrintVersion();
        await InitSchema();
        await InitCache();
        
        app.UseStaticFiles();
        var options = new RewriteOptions();
        options.AddRedirect(@"^admin$", $"{FluentCmsContentRoot}/admin");
        options.AddRedirect(@"^schema$", $"{FluentCmsContentRoot}/schema-ui/list.html");
        app.UseRewriter(options);
        
        UseSubApp("/admin");
        UseServerRouters();
        UseHomePage();
        return;

        void UseServerRouters()
        {
            app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");
            app.MapControllers();
        }

        void UseHomePage()
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
                            html = await pageService.Get(PageConstants.HomePage, new Dictionary<string, StringValues>());
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

        async Task InitCache()
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IEntitySchemaService>().ReplaceCache();
        }
        
        async Task InitSchema()
        {
            using var serviceScope = app.Services.CreateScope();

            var schemaService = serviceScope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable(default);
            await schemaService.EnsureTopMenuBar(default);
        }

        void UseSubApp(string  path)
        {
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{FluentCmsContentRoot}{path}"), subApp =>
            {
                subApp.UseRouting();
                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{path}", $"{FluentCmsContentRoot}{path}/index.html");
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{path}/{{*path:nonfile}}", $"{FluentCmsContentRoot}{path}/index.html");
                });
            });
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