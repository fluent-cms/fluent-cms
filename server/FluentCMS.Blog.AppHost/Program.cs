var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("redis").WithRedisCommander();
builder.AddProject<Projects.FluentCMS_Blog>("web").WithReference(redis);
builder.Build().Run();
