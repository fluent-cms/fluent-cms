using FluentCMS.Blog.Share;
var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis(name:CmsConstants.AspireRedis);
var db = builder.AddPostgres(CmsConstants.AspirePostgres);

if (args.FirstOrDefault() is "distribute")
{
    for (var i = 0; i < 2; i++)
    {
        builder.AddProject<Projects.FluentCMS_Blog>(name: "web" + i, launchProfileName: "http" + i)
            .WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.AspirePostgres)
            .WithReference(redis)
            .WithReference(db);
    }
}
else
{
    builder.AddProject<Projects.FluentCMS_Blog>(name: "web")
        .WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.AspirePostgres)
        .WithReference(redis)
        .WithReference(db)
        .WithReplicas(2);
}

builder.Build().Run();