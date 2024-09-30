using System.Runtime.InteropServices;
using FluentCMS.Auth.Services;
using FluentCMS.Blog.Data;
using FluentCMS.Services;
using FluentCMS.WebAppExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string corsPolicyName = "AllowAllOrigins";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
AddCors();

var (databaseProvider, connectionString) = GetProviderAndConnectionString();
AddCms();
AddDbContext();
builder.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();

var app = builder.Build();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseCors(corsPolicyName);
    app.UseSwagger();
    app.UseSwaggerUI();
}
await Migrate();
await app.UseCmsAsync();
app.UseCmsAuth<IdentityUser>();
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]));
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]));
app.Run();
return;

/////////////////////////////////////////////////
void AddCms()
{
    var staticAssetRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "wwwroot-win" : "wwwroot";

    switch (databaseProvider)
    {
        case "Sqlite":
            builder.AddSqliteCms(connectionString, staticAssetRoot);
            break;
        case "Postgres":
            builder.AddPostgresCms(connectionString, staticAssetRoot);
            break;
        case "SqlServer":
            builder.AddSqlServerCms(connectionString,staticAssetRoot);
            break;
        default:
            throw new Exception($"unknown provider {databaseProvider}");
    }
}

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
    await ctx.Database.EnsureCreatedAsync();
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
        case "SqlServer":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            break;
        default:
            throw new Exception($"Not supported Provider {databaseProvider}");
    }
}