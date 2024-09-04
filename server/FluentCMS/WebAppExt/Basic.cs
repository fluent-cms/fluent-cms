using System.IO.Compression;
using System.Text.Json.Serialization;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Services;
using FluentCMS.Components;
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

public static class Basic
{
    public static void AddPostgresCms<T>(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms<T>(builder, DatabaseProvider.Postgres, connectionString);

    public static void AddSqliteCms<T>(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms<T>(builder, DatabaseProvider.Sqlite, connectionString);

    public static void AddSqlServerCms<T>(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms<T>(builder, DatabaseProvider.SqlServer, connectionString);

    public static void RegisterCmsHook(this WebApplication app, string entityName, Occasion[] occasion, Delegate func)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.AddHooks(entityName, occasion, func);
    }

    public static async Task UseCmsAsync(this WebApplication app)
    {
        await InitSchema();
        await UseStatic();
        MapAdmin();

        app.UseRouting();
        app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");
        app.MapControllers();

        app.UseAntiforgery();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        async Task InitSchema()
        {
            using var scope = app.Services.CreateScope();

            var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable(default);
            await schemaService.EnsureTopMenuBar(default);
        }

        void MapAdmin()
        {
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/admin"), subApp =>
            {
                subApp.UseRouting();
                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile("/admin/", "index.html");
                    endpoints.MapFallbackToFile("/admin/{*path:nonfile}", "index.html");
                });
            });
        }

        async Task UseStatic()
        {
            if (!Directory.Exists("wwwroot/schema-ui"))
            {
                Console.WriteLine("***********************************************************************");
                Console.WriteLine("Wwwroot directory is empty, download fluent-cms client files to wwwroot");
                await DownloadAndExtractFilesFromGitHub();
                Console.WriteLine("FluentCMS client files are downloaded to wwwroot, please start the app again");
                Console.WriteLine("***********************************************************************");
                Environment.Exit(0);
            }
            app.UseStaticFiles();
        }

        async Task DownloadAndExtractFilesFromGitHub()
        {
            const string zipUrl = "https://github.com/fluent-cms/fluent-cms/raw/main/client_release/wwwroot.zip";
            var tempFile = Path.Combine(Path.GetTempPath(), "wwwroot.zip");
            var extractPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");


            using (var httpClient = new HttpClient())
            {
                // Download the zip file
                var response = await httpClient.GetAsync(zipUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
            }

            // Extract the files
            ZipFile.ExtractToDirectory(tempFile, Path.GetTempPath(), true);

            // Move extracted files to wwwroot
            var extractedDir = Path.Combine(Path.GetTempPath(), "wwwroot");
            if (Directory.Exists(extractedDir))
            {
                Directory.Move(extractedDir, extractPath);
            }

            // Clean up the temp file
            File.Delete(tempFile);
        }
    }

    private static void BuildCms<T>(WebApplicationBuilder builder, DatabaseProvider databaseProvider,
        string connectionString)
    {
        AddRouters();
        InjectDbServices();
        InjectServices();
        PrintVersion();

        void AddRouters()
        {
            builder.Services.AddRazorPages();
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddRouting(options => { options.LowercaseUrls = true; });
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        }

        void InjectServices()
        {
            builder.Services.AddSingleton(new AssemblyProvider { AppAssembly = typeof(T).Assembly });
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
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine("*********************************************************");
        }
    }
}