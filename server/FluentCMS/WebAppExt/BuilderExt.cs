using FluentCMS.Modules;
using FluentCMS.Utils.Nosql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.WebAppExt;

public static class BuilderExt
{
    public static void AddPostgresCms(this WebApplicationBuilder builder, string connectionString)
    {
        CmsModule.AddCms(builder, DatabaseProvider.Postgres, connectionString);
    }

    public static void AddSqliteCms(this WebApplicationBuilder builder, string connectionString)
    {
        CmsModule.AddCms(builder, DatabaseProvider.Sqlite, connectionString);
    }

    public static void AddSqlServerCms(this WebApplicationBuilder builder, string connectionString)
    {
        CmsModule.AddCms(builder, DatabaseProvider.SqlServer, connectionString);
    }

    public static void AddCmsAuth<TUser, TRole, TContext>(this WebApplicationBuilder builder)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        AuthModule<TUser>.AddCmsAuth<TUser, TRole, TContext>(builder);
    }

    public static void AddKafkaMessageProducer(this WebApplicationBuilder builder, string brokerList)
    {
        EventProduceModule.AddKafkaMessageProducer(builder,brokerList);
    }

    public static void AddMongoView(this WebApplicationBuilder builder, MongoConfig config)
    {
        MongoViewModule.AddMongoView(builder,config);
    }
}