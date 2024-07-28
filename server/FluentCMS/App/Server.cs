using System.Text.Json.Serialization;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Microsoft.Extensions.Caching.Memory;

namespace FluentCMS.App;

public sealed class EndPoint()
{
    public ControllerActionEndpointConventionBuilder ActionEndpoint { get; internal set; } = null!;
    public RouteGroupBuilder GroupBuilder { get; internal set; } = null!;
}
public sealed class Server
{
    private enum DataBaseProvider
    {
        Sqlite,
        Postgres,
    }
    
    private DataBaseProvider _databaseProvider ;
    private string _connectionString ="";
    private Server(){}

     public static Server UsePostgres(string connectionString)
     {
         return new Server
         {
             _connectionString = connectionString,
             _databaseProvider = DataBaseProvider.Postgres
         };
     }
     
    public static Server UseSqlite(string connectionString)
    {
        return new Server
        {
            _connectionString = connectionString,
            _databaseProvider = DataBaseProvider.Sqlite
        };
    }
    
    public void PrintVersion()
    {
        var parts = _connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();

        Console.WriteLine("*********************************************************");
        Console.WriteLine("Fluent CMS: version 0.1, build Jul21 4pm");
        Console.WriteLine($"Resolved Database Provider: {_databaseProvider}");
        Console.WriteLine($"Connection String: {String.Join(";", parts)}");
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine("*********************************************************");
    }

    public Result Build(WebApplicationBuilder builder)
    {
        var res = InjectDbServices(builder);
        if (res.IsFailed)
        {
            return Result.Fail(res.Errors);
        }
        InjectServices(builder);
        InitController(builder);
        return Result.Ok();
    }

    public async Task<EndPoint> Use(WebApplication app) 
    {
        using var scope = app.Services.CreateScope();
        var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
        await schemaService.AddSchemaTable();
        await schemaService.AddTopMenuBar();
        app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/error-development" : "/error");

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");
        var group = app.MapGroup("/api");
        var endpoint = app.MapControllers();
        return new EndPoint { GroupBuilder = group, ActionEndpoint = endpoint };
    }

    private void InitController(WebApplicationBuilder builder)
    {
        // Add services to the container.
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
        builder.Services.AddSingleton<MemoryCacheFactory>();
        builder.Services.AddSingleton<KeyValCache<View>>(p =>
            new KeyValCache<View>(p.GetRequiredService<IMemoryCache>(), 30, "view"));
        builder.Services.AddSingleton<LocalFileStore>(p => new LocalFileStore("wwwroot/files"));
        builder.Services.AddSingleton<KateQueryExecutor>();
        builder.Services.AddScoped<ISchemaService, SchemaService>();
        builder.Services.AddScoped<IEntityService, EntityService >();
        builder.Services.AddScoped<IViewService, ViewService >();
    }

    private Result InjectDbServices(WebApplicationBuilder builder)
    {
        switch (_databaseProvider)
        {
            case  DataBaseProvider.Sqlite:
                builder.Services.AddSingleton<IKateProvider>(p =>
                    new SqliteKateProvider(_connectionString, p.GetRequiredService<ILogger<SqliteKateProvider>>()));
                builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                    new SqliteDefinitionExecutor(_connectionString,
                        p.GetRequiredService<ILogger<SqliteDefinitionExecutor>>()));
                    
                break;
            case DataBaseProvider.Postgres:
                builder.Services.AddSingleton<IKateProvider>(p =>
                    new PostgresKateProvider(_connectionString, p.GetRequiredService<ILogger<PostgresKateProvider>>()));
                builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                    new PostgresDefinitionExecutor(_connectionString,
                        p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>()));
                break;
            default:
                return  Result.Fail($"Not supported Database Provider {_databaseProvider}, support [Sqlite, Postgres]");
        }
        return Result.Ok();
    }
}