using FluentCMS.Auth.models;
using FluentCMS.Blog.Share;
using FluentCMS.Exceptions;
using FluentCMS.WebAppExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

const string cors = "CorsOrigins";

var builder = WebApplication.CreateBuilder(args);
AddCorsPolicy();
AddHybridCache();
builder.AddServiceDefaults();
var provider = builder.Configuration.GetValue<string>(CmsConstants.DatabaseProvider)
               ?? throw new Exception("DatabaseProvider not found");
    
//both key Sqlite and ConnectionString_Sqlite work
var conn = Environment.GetEnvironmentVariable(provider) ??
           builder.Configuration.GetConnectionString(provider) ?? 
           throw new Exception("Connection string not found"); 

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

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseCors(cors);

await EnsureDbCreatedAsync();
InvalidParamExceptionFactory.Ok(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [RoleConstants.Sa]));
InvalidParamExceptionFactory.Ok(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [RoleConstants.Admin]));
await app.UseCmsAsync();

app.Run();
return;

void AddHybridCache()
{
    if (builder.Configuration.GetConnectionString(CmsConstants.Redis) is null) return;
    builder.AddRedisDistributedCache(connectionName: CmsConstants.Redis);
    builder.Services.AddHybridCache();
}

void AddCorsPolicy()
{
    var origins = builder.Configuration.GetValue<string>(cors);
    if (string.IsNullOrWhiteSpace(origins)) return;
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(cors, policy =>
        {
            policy.WithOrigins(origins.Split(","))
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });
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