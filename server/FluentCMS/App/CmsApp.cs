using System.Text.Json.Serialization;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.MessageProducer;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.App;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public sealed class CmsApp(WebApplicationBuilder builder,DatabaseProvider databaseProvider, string connectionString)
{
    private readonly HookRegistry _hookRegistry = new ();
    public void PrintVersion()
    {
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();

        Console.WriteLine("*********************************************************");
        Console.WriteLine("Fluent CMS: version 0.1, build Jul21 4pm");
        Console.WriteLine($"Resolved Database Provider: {databaseProvider}");
        Console.WriteLine($"Connection String: {String.Join(";", parts)}");
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine("*********************************************************");
    }

    public CmsApp Build()
    {
        InjectDbServices();
        InjectServices();
        InitController();
        return this;
    }

    private void InitController()
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
    
    private void InjectServices()
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<HookRegistry>(_=> _hookRegistry);
        builder.Services.AddSingleton<KeyValCache<View>>(p =>
            new KeyValCache<View>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
        builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore("wwwroot/files"));
        builder.Services.AddSingleton<KateQueryExecutor>(p =>
            new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), 30));
        builder.Services.AddScoped<ISchemaService, SchemaService>();
        builder.Services.AddScoped<IEntityService, EntityService >();
        builder.Services.AddScoped<IViewService, ViewService >();
    }

    private void InjectDbServices()
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
}

public static class WebApplicationExtensions
{
    public static CmsApp AddPostgresCms(this WebApplicationBuilder builder, string connectionString) =>
        new CmsApp(builder, DatabaseProvider.Postgres, connectionString).Build();

    public static CmsApp AddSqliteCms(this WebApplicationBuilder builder, string connectionString) =>
        new CmsApp(builder, DatabaseProvider.Sqlite, connectionString).Build();

    public static CmsApp AddSqlServerCms(this WebApplicationBuilder builder, string connectionString) =>
        new CmsApp(builder, DatabaseProvider.SqlServer, connectionString).Build();

    public static void AddKafkaMessageProducer(this WebApplicationBuilder builder, string brokerList)
    {
        builder.Services.AddSingleton<IMessageProducer>(p =>
            new KafkaMessageProducer(brokerList, p.GetRequiredService<ILogger<KafkaMessageProducer>>()));
        builder.Services.AddSingleton<ProducerHookRegister>();
    }

    public static IEntityService GetCmsEntityService(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IEntityService>();
    }

    public static ISchemaService GetCmsSchemaService(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISchemaService>();
    }

    public static HookRegistry GetCmsHookFactory(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<HookRegistry>();
    }

    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*")
    {
        var producerHookRegister = app.Services.GetRequiredService<ProducerHookRegister>();
        producerHookRegister.RegisterMessageProducer(entityName);
    }

    public static async Task UseCmsAsync(this WebApplication app, bool requireAuth)
    {
        app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        var endpoint = app.MapControllers();
        if (requireAuth)
        {
            endpoint.RequireAuthorization();
        }
        else
        {
            //tell admin panel no need to login
            app.MapGet("/api/manage/info", () => new { Email = "admin@cms.com" });
        }


        using var scope = app.Services.CreateScope();
        var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
        await schemaService.EnsureSchemaTable(default);
        await schemaService.EnsureTopMenuBar(default);

    }
}