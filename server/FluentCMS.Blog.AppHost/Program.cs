var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("redis");
builder.AddProject<Projects.FluentCMS_Blog>("web")
    .WithReplicas(2)
    .WithReference(redis);
builder.Build().Run();