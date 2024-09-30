using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.WebAppExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.App;


public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public class CmsApp(ILogger<CmsApp> logger, DatabaseProvider databaseProvider, string connectionString, string environmentName)
{
    private const string StaticFileRoot = "wwwroot";
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
                p.GetRequiredService<ILogger<CmsApp>>(), 
                databaseProvider, 
                connectionString,
                builder.Environment.EnvironmentName
                )
            );
            builder.Services.AddSingleton<Renderer>(_ => new Renderer(Path.Combine(Directory.GetCurrentDirectory(),StaticFileRoot,"static-assets/templates/template.html")));
            builder.Services.AddSingleton<HookRegistry>(_ => new HookRegistry());
            builder.Services.AddSingleton<ImmutableCache<Query>>(p =>
                new ImmutableCache<Query>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
            builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore(Path.Combine(Directory.GetCurrentDirectory(),StaticFileRoot,"files")));
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
        PrintVersion();
        await InitSchema();
        UseStatic();
        UserAdminPanel();
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
                        var html = """<a href="/admin">Log in to Admin</a>""";
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

        void UserAdminPanel()
        {
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/admin"), subApp =>
            {
                subApp.UseRouting();
                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile("/admin/", "admin/index.html");
                    endpoints.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");
                });
            });
        }

        void UseStatic()
        {
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(),StaticFileRoot, "admin"))  
                && !Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(),StaticFileRoot ,"schema-ui")))
            {
                logger.LogInformation($"Can not find FluentCMS client files, copying files to {StaticFileRoot}");
                var res = CopyPackageFiles();
                logger.LogWarning(res
                    ? $"FluentCMS client files are copied to {StaticFileRoot}, please start the app again"
                    : $"Copy FluentCMS client files fail, please manually copy file from .nuget/packages/fluentcms to {StaticFileRoot}");

                Environment.Exit(0);
            }

            app.UseStaticFiles();
        }

        bool CopyPackageFiles()
        {
            var filePattern = "*.staticwebassets.runtime.json";
            var files = Directory.GetFiles(GetAssemblyPath(), filePattern, SearchOption.TopDirectoryOnly);
            if (files.Any())
            {
                var content = File.ReadAllText(files.First());
                var rootConfig = JsonSerializer.Deserialize<ContentRootConfig>(content);
                var source = rootConfig?.ContentRoots.FirstOrDefault(x => x.Contains("fluentcms"));
                if (!string.IsNullOrWhiteSpace(source))
                {
                    var target = Path.Combine(Directory.GetCurrentDirectory(), StaticFileRoot);
                    foreach (var path in new[] { "schema-ui", "admin", "static-assets" })
                    {
                        CopyDirectory(Path.Combine(source, path), Path.Combine(target, path));
                        CopyDirectory(Path.Combine(source, path), Path.Combine(target, path));
                        CopyDirectory(Path.Combine(source, path), Path.Combine(target, path));
                    }

                    if (!File.Exists(Path.Combine(target, "favicon.ico")))
                    {
                        File.Copy(Path.Combine(source, "favicon.ico"), Path.Combine(target, "favicon.ico"));
                    }

                    return true;
                }
            }
            else
            {
               logger.LogWarning("Can not find file staticwebassets.runtime.json");
            }

            return false;
        }

        void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationDir, fileName);
                try
                {
                    File.Copy(file, destFile, overwrite: true);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error copying file '{file}': {ex.Message}");
                }
            }

            // Recursively copy all subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var subDirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destinationDir, subDirName);
                CopyDirectory(subDir, destSubDir);
            }
        }
    }

    private void PrintVersion()
    {
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();
        logger.LogInformation("*********************************************************");
        logger.LogInformation($"Fluent CMS, {environmentName}");
        logger.LogInformation($"Resolved Database Provider: {databaseProvider}");
        logger.LogInformation($"Connection String: {string.Join(";", parts)}");
        logger.LogInformation($"Current Location: {Directory.GetCurrentDirectory()}");
        logger.LogInformation($"Static Asset Root:{StaticFileRoot}");
        logger.LogInformation($"Fluent CMS Package Location:{GetAssemblyPath()}");
        logger.LogInformation("*********************************************************");
    }
    
    private static string GetAssemblyPath()
    {
        var assemblyPath = Assembly.GetAssembly(typeof(InvalidParamException))!.Location;
        return Path.GetDirectoryName(assemblyPath)!;
    }
}