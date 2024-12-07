var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.FluentCMS_Blog>("blog");
builder.Build().Run();
