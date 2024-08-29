using System.IO.Compression;
using System.Text.Json.Serialization;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Services;
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
    public static void AddPostgresCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.Postgres, connectionString);

    public static void AddSqliteCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.Sqlite, connectionString);

    public static void AddSqlServerCms(this WebApplicationBuilder builder, string connectionString) =>
        BuildCms(builder, DatabaseProvider.SqlServer, connectionString);
    public static void RegisterCmsHook(this WebApplication app, string entityName, Occasion[] occasion,Delegate func)
    {
         var registry = app.Services.GetRequiredService<HookRegistry>();
         registry.AddHooks(entityName, occasion, func);
    }
    
    public static async Task UseCmsAsync(this WebApplication app)
    {
        if (!Directory.Exists("wwwroot"))
        {
            Console.WriteLine("wwwroot directory is empty, download fluent-cms client files to wwwroot");
            await DownloadAndExtractFilesFromGitHub();
            Console.WriteLine("fluent-cms client files was downloaded to wwwroot");
            Console.WriteLine("please restart the application");
            Environment.Exit(0);
        }
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");
        app.MapControllers();
        using var scope = app.Services.CreateScope();

        var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
        await schemaService.EnsureSchemaTable(default);
        await schemaService.EnsureTopMenuBar(default);
    }

    private static async Task DownloadAndExtractFilesFromGitHub()
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
    private static void BuildCms(WebApplicationBuilder builder, DatabaseProvider provider, string connectionString )
    {
        InitController(builder);
        InjectDbServices(builder, provider, connectionString);
        InjectServices(builder);
        PrintVersion(provider,connectionString, builder.Environment.EnvironmentName);
    }
    private static void InitController(WebApplicationBuilder builder)
    {
        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        }); 
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    }
    
    private static void InjectServices(WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<HookRegistry>(_=> new HookRegistry());
        builder.Services.AddSingleton<ImmutableCache<Query>>(p =>
            new ImmutableCache<Query>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
        builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore("wwwroot/files"));
        builder.Services.AddSingleton<KateQueryExecutor>(p =>
            new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), 30));
        builder.Services.AddScoped<ISchemaService, SchemaService>();
        builder.Services.AddScoped<IEntityService, EntityService >();
        builder.Services.AddScoped<IViewService, QueryService >();
        builder.Services.AddScoped<IProfileService, DummyProfileService >();
    }

    private static void InjectDbServices(WebApplicationBuilder builder, DatabaseProvider databaseProvider, string connectionString)
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
                    new PostgresKateProvider(connectionString, p.GetRequiredService<ILogger<PostgresKateProvider>>()));
                builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                    new PostgresDefinitionExecutor(connectionString,
                        p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>()));
                break;
            case DatabaseProvider.SqlServer:
                builder.Services.AddSingleton<IKateProvider>(p =>
                    new SqlServerKateProvider(connectionString, p.GetRequiredService<ILogger<SqlServerKateProvider>>()));
                builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                    new SqlServerDefinitionExecutor(connectionString,
                        p.GetRequiredService<ILogger<SqlServerDefinitionExecutor>>()));
                break;
        }
    }
    private static void PrintVersion(DatabaseProvider databaseProvider, string connectionString, string environment)
    {
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();

        Console.WriteLine("*********************************************************");
        Console.WriteLine($"Fluent CMS, {environment}");
        Console.WriteLine($"Resolved Database Provider: {databaseProvider}");
        Console.WriteLine($"Connection String: {string.Join(";", parts)}");
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine("*********************************************************");
    }
}