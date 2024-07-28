using FluentCMS.App;
using FluentCMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string corsPolicyName = "AllowAllOrigins";
var builder = WebApplication.CreateBuilder(args);
var (databaseProvider, connectionString) = GetProviderAndConnectionString();

var cmsServer = databaseProvider switch
{
    "Sqlite" => Server.UseSqlite(connectionString),
    "Postgres" => Server.UsePostgres(connectionString),
    _ => throw new Exception("not support")
};

cmsServer.PrintVersion();

var buildResult = cmsServer.Build(builder);
if (buildResult.IsFailed)
{
    Console.WriteLine(buildResult.Errors);
    return;
};

AddDbContext();
AddCors();

builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseCors(corsPolicyName);
}
app.UseAuthorization();
await Migrate();

var endPoint = await cmsServer.Use(app);
endPoint.GroupBuilder.MapIdentityApi<IdentityUser>();
endPoint.ActionEndpoint.RequireAuthorization();
app.Run();

return;

(string, string) GetProviderAndConnectionString()
{
    var provider = builder.Configuration.GetValue<string>("DatabaseProvider");
    if (string.IsNullOrWhiteSpace(provider))
    {
        throw new Exception("Not find DatabaseProvider");
    }

    //both key Sqlite and ConnectionString_Sqlite work
    var connection = Environment.GetEnvironmentVariable(provider) ??
                           builder.Configuration.GetConnectionString(provider);
    if (string.IsNullOrWhiteSpace(connection))
    {
        throw new Exception("Not find connection string");
    }  
    return (provider, connection);
}

void AddCors()
{
    var origins = builder.Configuration.GetValue<string>("AllowedOrigins");
    if (!string.IsNullOrWhiteSpace(origins))
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName,
                policy =>
                {
                    policy.WithOrigins(origins.Split(",")).AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }
}

async Task Migrate()
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var first = ctx.Database.GetAppliedMigrations().FirstOrDefault();
    if (string.IsNullOrWhiteSpace(first))
    {
        //it's save to do migrate, because database is empty
        await ctx.Database.MigrateAsync();
    }
}

void AddDbContext()
{
    switch (databaseProvider)
    {
        case "Sqlite":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
            break;
        case "Postgres":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            break;
        default:
            throw new Exception($"Not supported Provider {databaseProvider}");
    }
}