using FluentCMS.Blog.Share;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis(name:CmsConstants.Redis);
var (isTesting, useSqlServer) = (builder.Configuration.GetValue("IsTesting", false), builder.Configuration.GetValue("UseSqlServer", false));
var databaseProvider = useSqlServer ? CmsConstants.SqlServer : CmsConstants.Postgres;

IResourceBuilder<IResourceWithConnectionString> db = useSqlServer
    ? builder.AddSqlServer(CmsConstants.SqlServer)
    : builder.AddPostgres(CmsConstants.Postgres);

if (isTesting) return RunAsTwoSeparateApps();

builder.AddProject<Projects.FluentCMS_Blog>(name: "web")
    .WithEnvironment(CmsConstants.DatabaseProvider, databaseProvider)
    .WithReference(redis)
    .WithReference(db)
    .WithReplicas(2);
builder.Build().Run();
return 0;

int RunAsTwoSeparateApps()
{
    for (var i = 0; i < 2; i++)
    {
        builder.AddProject<Projects.FluentCMS_Blog>(name: "web" + i, launchProfileName: "http" + i)
            .WithEnvironment(CmsConstants.DatabaseProvider, databaseProvider)
            .WithReference(redis)
            .WithReference(db);
    }

    builder.Build().Run();
    return 0;
}