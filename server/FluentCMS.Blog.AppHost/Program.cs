using FluentCMS.Blog.Share;

var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis(name:CmsConstants.AspireRedis);
var db = builder.AddPostgres(CmsConstants.AspirePostgres);
// test hybrid cache 
/*
for (var i = 0; i < 2; i++)
{
    builder.AddProject<Projects.FluentCMS_Blog>(name: "web" + i,launchProfileName:"http" + i)
        .WithReference(db).WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.AspirePostgres)
        .WithReference(redis);
}
*/

// load balancing
 builder.AddProject<Projects.FluentCMS_Blog>(name:"web")
     .WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.AspirePostgres)
     .WithReference(redis)
     .WithReference(db)
     .WithReplicas(2);

builder.Build().Run();