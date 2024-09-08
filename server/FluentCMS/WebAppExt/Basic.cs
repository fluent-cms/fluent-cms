using System.IO.Compression;
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
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.WebAppExt;
public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public class ContentRootConfig
{
    public string[] ContentRoots { get; set; } = [];
}

public static class Basic
{
    public static void AddPostgresCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.Postgres, connectionString);

    public static void AddSqliteCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.Sqlite, connectionString);

    public static void AddSqlServerCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.SqlServer, connectionString);

    public static void RegisterCmsHook(this WebApplication app, string entityName, Occasion[] occasion, Delegate func)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.AddHooks(entityName, occasion, func);
    }

    public static async Task UseCmsAsync(this WebApplication app)
    {
        await InitSchema();
        UseStatic();
        UserAdminPanel();
        UseServerRouters();
        UseHomePage();

        void UseServerRouters()
        {
            app.UseRouting();
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
                            html = await pageService.Get(Page.HomePage);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
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
            if (!Directory.Exists("wwwroot"))
            {
                Console.WriteLine("***********************************************************************");
                Console.WriteLine("Can not find FluentCMS client files, copying files to wwwroot");
                var res = CopyPackageFiles();
                if (res)
                {
                    Console.WriteLine("FluentCMS client files are copied to wwwroot, please start the app again");
                    Console.WriteLine("***********************************************************************");
                }
                else
                {
                    Console.WriteLine("Copy FluentCMS client files fail, please manually copy file from .nuget/packages/fluentcms to wwwroot");
                    Console.WriteLine("***********************************************************************");
                }
                Environment.Exit(0);
            }

            app.UseStaticFiles();
        }

        bool CopyPackageFiles()
        {
            string filePattern = "*.staticwebassets.runtime.json";
            var files = Directory.GetFiles( GetAssemblyPath(), filePattern, SearchOption.TopDirectoryOnly);
            if (files.Any())
            {
                var content = File.ReadAllText(files.First());
                var rootConfig = JsonSerializer.Deserialize<ContentRootConfig>(content);
                if (rootConfig?.ContentRoots.Length > 0)
                {
                    CopyDirectory(rootConfig.ContentRoots.Last(), Path.Combine(Directory.GetCurrentDirectory(),"wwwroot") );
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Can not find file staticwebassets.runtime.json");
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
                    Console.WriteLine($"Error copying file '{file}': {ex.Message}");
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

    
    private static string GetAssemblyPath()
    {
        var assemblyPath = Assembly.GetAssembly(typeof(InvalidParamException))!.Location;
        return Path.GetDirectoryName(assemblyPath)!;
    }

    private static void BuildCms(WebApplicationBuilder builder, DatabaseProvider databaseProvider,
        string connectionString)
    {
        AddRouters();
        InjectDbServices();
        InjectServices();
        PrintVersion();

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
            builder.Services.AddSingleton<HookRegistry>(_ => new HookRegistry());
            builder.Services.AddSingleton<ImmutableCache<Query>>(p =>
                new ImmutableCache<Query>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
            builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore("wwwroot/files"));
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
            }
        }

        void PrintVersion()
        {
            var parts = connectionString.Split(";")
                .Where(x => !x.StartsWith("Password"))
                .ToArray();
           Console.WriteLine("*********************************************************");
            Console.WriteLine($"Fluent CMS, {builder.Environment.EnvironmentName}");
            Console.WriteLine($"Resolved Database Provider: {databaseProvider}");
            Console.WriteLine($"Connection String: {string.Join(";", parts)}");
            Console.WriteLine($"Current Location: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"Fluent CMS Package Location:{GetAssemblyPath()}");
            Console.WriteLine("*********************************************************");
        }
    }
}