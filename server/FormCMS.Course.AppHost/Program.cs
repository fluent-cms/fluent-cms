using FormCMS.Course;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis(name:Constants.Redis);
var (isTesting, useSqlServer) = (builder.Configuration.GetValue("IsTesting", false), builder.Configuration.GetValue("UseSqlServer", false));
var databaseProvider = useSqlServer ? Constants.SqlServer : Constants.Postgres;

IResourceBuilder<IResourceWithConnectionString> db = useSqlServer
    ? builder.AddSqlServer(Constants.SqlServer)
    : builder.AddPostgres(Constants.Postgres);

if (isTesting) return RunAsTwoSeparateApps();

builder.AddProject<Projects.FormCMS_Course>(name: "web")
    .WithEnvironment(Constants.DatabaseProvider, databaseProvider)
    .WithReference(redis)
    .WithReference(db)
    .WithReplicas(2);
builder.Build().Run();
return 0;

int RunAsTwoSeparateApps()
{
    for (var i = 0; i < 2; i++)
    {
        builder.AddProject<Projects.FormCMS_Course>(name: "web" + i, launchProfileName: "http" + i)
            .WithEnvironment(Constants.DatabaseProvider, databaseProvider)
            .WithReference(redis)
            .WithReference(db);
    }

    builder.Build().Run();
    return 0;
}