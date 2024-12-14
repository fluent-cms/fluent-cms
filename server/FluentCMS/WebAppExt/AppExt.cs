using FluentCMS.Builders;
using FluentCMS.Options;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.WebAppExt;

public static class AppExt
{
    public static async Task UseCmsAsync(this WebApplication app)
    {
        await app.Services.GetRequiredService<Builders.CmsBuilder>().UseCmsAsync(app);
        app.Services.GetService<IAuthBuilder>()?.UseCmsAuth(app);
    }

    public static HookRegistry GetHookRegistry(this WebApplication app) =>
        app.Services.GetRequiredService<HookRegistry>();

    public static async Task<Result> EnsureCmsUser(this WebApplication app, string email, string password,
        string[] role) =>
        await app.Services.GetRequiredService<IAuthBuilder>().EnsureCmsUser(app, email, password, role);

    public static void RegisterMongoViewHook(this WebApplication app, string viewName = "*") =>
        app.Services.GetRequiredService<MongoQueryBuilder>().RegisterMongoViewHook(app, viewName);

    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*") =>
        app.Services.GetRequiredService<EventProduceBuilder>().RegisterMessageProducerHook(app, entityName);

    public static IServiceCollection AddPostgresCms(this IServiceCollection services, string connectionString, Action<CmsOptions>? action = default)
    {
        return Builders.CmsBuilder.AddCms(services, DatabaseProvider.Postgres, connectionString,action);
    }

    public static IServiceCollection AddSqliteCms(this  IServiceCollection services, string connectionString, Action<CmsOptions>? action = default)
    {
        return Builders.CmsBuilder.AddCms(services, DatabaseProvider.Sqlite, connectionString,action);
    }

    public static IServiceCollection AddSqlServerCms(this IServiceCollection services, string connectionString,Action<CmsOptions>? action = default) 
    {
        return Builders.CmsBuilder.AddCms(services, DatabaseProvider.SqlServer, connectionString, action);
    }

    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(this IServiceCollection services)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        return AuthBuilder<TUser>.AddCmsAuth<TUser, TRole, TContext>(services);
    }

    public static IServiceCollection AddKafkaMessageProducer(this IServiceCollection services, string brokerList)
    {
        return EventProduceBuilder.AddKafkaMessageProducer(services,brokerList);
    }

    public static IServiceCollection AddMongoView(this IServiceCollection services, MongoConfig config)
    {
        return MongoQueryBuilder.AddMongoView(services,config);
    }
}