using FluentCMS.App;
using FluentCMS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string corsPolicyName = "AllowAllOrigins";
var builder = WebApplication.CreateBuilder(args);
var (databaseProvider, connectionString) = GetProviderAndConnectionString();

var cmsServer = databaseProvider switch
{
    "Sqlite" => builder.AddSqliteCms(connectionString),
    "Postgres" => builder.AddPostgresCms(connectionString),
    "SqlServer" =>builder.AddSqlServerCms(connectionString),
    _ => throw new Exception("not support")
};

cmsServer.PrintVersion();
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
await app.UseCmsAsync(true);
app.MapGroup("/api").MapIdentityApi<IdentityUser>();
app.MapGet("/api/logout", async (SignInManager<IdentityUser> signInManager) => await signInManager.SignOutAsync());

app.Run();

/////////////////////////////////////////////////
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