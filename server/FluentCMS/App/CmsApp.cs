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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.App;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public class CmsApp(
    WebApplicationBuilder builder,
    ILogger<CmsApp> logger, 
    DatabaseProvider databaseProvider, 
    string connectionString 
)
{
    private const string FluentCmsContentRoot = "/_content/FluentCMS";
    public static void Build(WebApplicationBuilder builder,DatabaseProvider databaseProvider, string connectionString)
    {
        AddRouters();
        InjectDbServices();
        InjectServices();

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
            builder.Services.AddSingleton<CmsApp>(p => new CmsApp(
                    builder,
                p.GetRequiredService<ILogger<CmsApp>>(), 
                databaseProvider, 
                connectionString
                )
            );
            builder.Services.AddSingleton<Renderer>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FluentCmsContentRoot}/static-assets/templates/template.html");
                return new Renderer(fileInfo.PhysicalPath??"");
            });
            builder.Services.AddSingleton<HookRegistry>(_ => new HookRegistry());
            builder.Services.AddSingleton<ImmutableCache<Query>>(p =>
                new ImmutableCache<Query>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
            builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore(Path.Combine(Directory.GetCurrentDirectory(),"wwwroot/files")));
            builder.Services.AddSingleton<KateQueryExecutor>(p =>
                new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), 30));
            builder.Services.AddScoped<ISchemaService, SchemaService>();
            builder.Services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            builder.Services.AddScoped<IQuerySchemaService, QuerySchemaService>();
            builder.Services.AddScoped<IEntityService, EntityService>();
            builder.Services.AddScoped<IQueryService, QueryService>();
            builder.Services.AddScoped<IProfileService, DummyProfileService>();
            builder.Services.AddScoped<IPageService, PageService>();
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
        var env = app.Services.GetRequiredService<IWebHostEnvironment>();
        PrintVersion();
        await InitSchema();
        app.UseStaticFiles();
        UseAdminPanel();
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
                                    <a href="{FluentCmsContentRoot}/admin">Log in to Admin</a><br/>
                                    <a href="{FluentCmsContentRoot}/schema-ui/list.html">Go to Schema Builder</a>
                                    """;
                        try
                        {
                            html = await pageService.Get(Page.HomePage, new Dictionary<string, StringValues>());
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
            using var scope = app.Services.CreateScope();

            var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable(default);
            await schemaService.EnsureTopMenuBar(default);
        }

        void UseAdminPanel()
        {
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{FluentCmsContentRoot}/admin"), subApp =>
            {
                subApp.UseRouting();
                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}/admin", $"{FluentCmsContentRoot}/admin/index.html");
                    endpoints.MapFallbackToFile($"{FluentCmsContentRoot}/admin/{{*path:nonfile}}", $"{FluentCmsContentRoot}/admin/index.html");
                });
            });
        }
    }

    private void PrintVersion()
    {
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();
        logger.LogInformation("*********************************************************");
        logger.LogInformation($"Fluent CMS, {builder.Environment.EnvironmentName}");
        logger.LogInformation($"Resolved Database Provider: {databaseProvider}");
        logger.LogInformation($"Connection String: {string.Join(";", parts)}");
        logger.LogInformation("*********************************************************");
    }
}