using FluentCMS.Auth.models;
using FluentCMS.Blog;
using FluentCMS.Types;
using FluentCMS.WebAppBuilders;
using FluentCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
const string cors = "cors";


var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration.GetValue<string>(Constants.DatabaseProvider) ??
               throw new Exception("DatabaseProvider not found");
var conn = builder.Configuration.GetConnectionString(provider) ?? 
           throw new Exception($"Connection string {provider} not found"); 

_ = provider switch
{
    Constants.Sqlite => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlite(conn))
        .AddSqliteCms(conn),
    Constants.Postgres => builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(conn))
        .AddPostgresCms(conn),
    Constants.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlServer(conn))
        .AddSqlServerCms(conn),
    _ => throw new Exception("Database provider not found")
};

builder.Services.AddCmsAuth<IdentityUser,IdentityRole,CmsDbContext>();
AddHybridCache();
AddOutputCachePolicy();
builder.AddServiceDefaults();
if (builder.Environment.IsDevelopment()) AddCorsPolicy();

var app = builder.Build();

await EnsureDbCreatedAsync();
await app.UseCmsAsync();

(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [RoleConstants.Sa])).Ok();
(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [RoleConstants.Admin])).Ok();

app.MapDefaultEndpoints();
app.UseOutputCache();
if (app.Environment.IsDevelopment()) app.UseCors(cors);

app.Run();
return;

void AddOutputCachePolicy()
{
    builder.Services.AddOutputCache(cacheOption =>
    {
        cacheOption.AddBasePolicy(policyBuilder => policyBuilder.Expire(TimeSpan.FromMinutes(1)));
        cacheOption.AddPolicy(CmsOptions.DefaultPageCachePolicyName,
            b => b.Expire(TimeSpan.FromMinutes(1)));
        cacheOption.AddPolicy(CmsOptions.DefaultQueryCachePolicyName,
            b => b.Expire(TimeSpan.FromSeconds(1)));
    });
}

void AddHybridCache()
{
    if (builder.Configuration.GetConnectionString(Constants.Redis) is null) return;
    builder.AddRedisDistributedCache(connectionName: Constants.Redis);
#pragma warning disable EXTEXP0018
    builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018
}

async Task EnsureDbCreatedAsync()
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    await ctx.Database.EnsureCreatedAsync();
}

void AddCorsPolicy()
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            cors,
            policy =>
            {
                policy.WithOrigins("http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
    });
}