using System.Text.Json.Serialization;
using FluentCMS.Services;
using FluentCMS.Data;
using Utils.DataDefinitionExecutor;
using Utils.File;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Utils.Cache;
using Utils.KateQueryExecutor;
using Utils.QueryBuilder;

var builder = WebApplication.CreateBuilder(args);

PrintVersion();
InjectDbServices();
InjectServices();
AddCors();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
}); 
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<AppDbContext>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseExceptionHandler("/error-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseCors("AllowAllOrigins");
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
var group = app.MapGroup("/api");
group.MapIdentityApi<IdentityUser>();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await InitDb();
app.Run();

string? ConnectionString(string key) =>
    Environment.GetEnvironmentVariable(key) ?? builder.Configuration.GetConnectionString(key);

string? ConfigurationString(string key) =>
    Environment.GetEnvironmentVariable(key) ?? builder.Configuration.GetValue<string>(key);

async Task InitDb()
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var first = ctx.Database.GetAppliedMigrations().FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(first))
    {
        Console.WriteLine("*********************************************************");
        Console.WriteLine("Database initialized, Ignore");
        Console.WriteLine("*********************************************************");
    }

    //now it's save to do migrate
    await ctx.Database.MigrateAsync();
    Console.WriteLine("*********************************************************");
    Console.WriteLine("Initializing database");
    Console.WriteLine("*********************************************************");
    var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
    await schemaService.AddTopMenuBar();
}

void InjectDbServices()
{
    var provider = Val.StrNotEmpty(ConfigurationString("DatabaseProvider"))
        .ValOrThrow("Not find Database Provider");

    var connectionString = Val.StrNotEmpty(ConnectionString(provider))
        .ValOrThrow($"Not  find Connection string for {provider}");

    IDefinitionExecutor? definitionExecutor = null;
    
    switch (provider)
    {
        case "Sqlite":
            
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
            builder.Services.AddSingleton<IKateProvider>(p =>
                new SqliteKateProvider(connectionString, p.GetRequiredService<ILogger<SqliteKateProvider>>()));
            builder.Services.AddSingleton<IDefinitionExecutor>(p =>
                {
                    definitionExecutor = new SqliteDefinitionExecutor(connectionString,
                        p.GetRequiredService<ILogger<SqliteDefinitionExecutor>>());
                    return definitionExecutor;
                });
            break;
        case "Postgres":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddSingleton<IKateProvider>(p =>
                new PostgresKateProvider(connectionString, p.GetRequiredService<ILogger<PostgresKateProvider>>()));
            builder.Services.AddSingleton<IDefinitionExecutor>(p =>
            {
                 definitionExecutor = new PostgresDefinitionExecutor(connectionString,
                    p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>());
                return definitionExecutor;
            });
            break;
        default:
            throw new Exception($"Not supported Provider {provider}");
    }



}

void PrintVersion()
{
    var provider = Val.StrNotEmpty(ConfigurationString("DatabaseProvider"))
        .ValOrThrow("Not find Database Provider");

    var connectionString = Val.StrNotEmpty(ConnectionString(provider))
        .ValOrThrow($"Not  find Connection string for {provider}");

     var parts = connectionString.Split(";")
         .Where(x => !x.StartsWith("Password"))
         .ToArray();
     
     Console.WriteLine("*********************************************************");
     Console.WriteLine($"Fluent CMS: version 0.1, build Jul21 4pm");
     Console.WriteLine($"Resolved Database Provider: {provider}");
     Console.WriteLine($"Connection String: {String.Join(";", parts)}");
     Console.WriteLine("*********************************************************");   
}

void AddCors()
{
    var origins = ConfigurationString("AllowedOrigins");
    if (!string.IsNullOrWhiteSpace(origins))
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy =>
                {
                    policy.WithOrigins(origins.Split(",")).AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }
}

void InjectServices()
{
    builder.Services.AddSingleton<MemoryCacheFactory>();
    builder.Services.AddSingleton<KeyValCache<View>>(p=> new KeyValCache<View>(p.GetRequiredService<IMemoryCache>(),30,"view"));
    builder.Services.AddSingleton<FileUtl>(p => new FileUtl("wwwroot/files"));
    builder.Services.AddSingleton<KateQueryExecutor>();
    builder.Services.AddScoped<ISchemaService, SchemaService>();
    builder.Services.AddScoped<IEntityService, EntityService >();
    builder.Services.AddScoped<IViewService, ViewService >();
}
