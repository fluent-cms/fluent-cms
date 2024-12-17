using FluentCMS.Auth.models;
using FluentCMS.Blog.Share;
using FluentCMS.Types;
using FluentCMS.Utils.ResultExt;
using FluentCMS.WebAppExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration.GetValue<string>(CmsConstants.DatabaseProvider) ??
               throw new Exception("DatabaseProvider not found");
var conn = builder.Configuration.GetConnectionString(provider) ?? 
           throw new Exception($"Connection string {provider} not found"); 

_ = provider switch
{
    CmsConstants.Sqlite => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlite(conn))
        .AddSqliteCms(conn),
    CmsConstants.Postgres => builder.Services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(conn))
        .AddPostgresCms(conn),
    CmsConstants.SqlServer => builder.Services.AddDbContext<CmsDbContext>(options => options.UseSqlServer(conn))
        .AddSqlServerCms(conn),
    _ => throw new Exception("Database provider not found")
};

builder.Services.AddCmsAuth<IdentityUser,IdentityRole,CmsDbContext>();
AddHybridCache();
AddOutputCachePolicy();
builder.AddServiceDefaults();

var app = builder.Build();

await EnsureDbCreatedAsync();
await app.UseCmsAsync();

(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [RoleConstants.Sa])).Ok();
(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [RoleConstants.Admin])).Ok();

app.MapDefaultEndpoints();
app.UseOutputCache();
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
    if (builder.Configuration.GetConnectionString(CmsConstants.Redis) is null) return;
    builder.AddRedisDistributedCache(connectionName: CmsConstants.Redis);
    builder.Services.AddHybridCache();
}

async Task EnsureDbCreatedAsync()
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    await ctx.Database.EnsureCreatedAsync();
}

internal class CmsDbContext : IdentityDbContext<IdentityUser>
{
    public CmsDbContext(){}
    public CmsDbContext(DbContextOptions<CmsDbContext> options):base(options){}
}