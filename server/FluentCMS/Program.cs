using System.Text.Json.Serialization;
using FluentCMS.Services;
using FluentCMS.Data;
using Utils.DataDefinitionExecutor;
using Utils.File;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Utils.KateQueryExecutor;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


InjectDb();
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

app.Run();

string? GetConnectionString(string key)
{
    var ret = builder.Configuration.GetConnectionString(key) ?? Environment.GetEnvironmentVariable(key);
    if (ret is not null)
    {
        Console.WriteLine("***********************************");
        Console.WriteLine($"Current Connection string is {ret}");
        Console.WriteLine("***********************************");
    }
    return ret;
}

void InjectDb()
{
    var isDebug = builder.Environment.IsDevelopment();
    var connectionString = GetConnectionString("Sqlite");
    if (connectionString is not null)
    {
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddSingleton<IKateProvider>(p => new SqliteKateProvider(connectionString,isDebug));
        builder.Services.AddSingleton<IDefinitionExecutor>(p => new SqliteDefinitionExecutor(connectionString, isDebug));
        return;
    }

    connectionString = GetConnectionString("Postgres");
    if (connectionString is not null)
    {
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton<IKateProvider>(p => new PostgresKateProvider(connectionString,isDebug));
        builder.Services.AddSingleton<IDefinitionExecutor>(p => new PostgresDefinitionExecutor(connectionString, isDebug));
        return;
    }


    throw new Exception("didn't find any connection settings");
}

void AddCors()
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins",
            policy =>
            {
                policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173").AllowAnyHeader()
                    .AllowCredentials();
            });
    });
}

void InjectServices()
{
    builder.Services.AddSingleton<FileUtl, FileUtl>(p => new FileUtl("wwwroot/files"));
    builder.Services.AddSingleton<KateQueryExecutor, KateQueryExecutor>();
    builder.Services.AddScoped<ISchemaService, SchemaService>();
    builder.Services.AddScoped<IEntityService, EntityService >();
    builder.Services.AddScoped<IViewService, ViewService >();
}
