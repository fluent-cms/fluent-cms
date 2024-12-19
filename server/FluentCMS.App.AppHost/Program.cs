using FluentCMS.App;

var builder = DistributedApplication.CreateBuilder(args);
var kafka = builder
    .AddKafka(AppConstants.Kafka);

var mongoCmsDb = builder
    .AddMongoDB("mongo")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false)
    .AddDatabase(AppConstants.MongoCms);

var postgresCmsDb = builder
    .AddPostgres(AppConstants.PostgresCms)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly:false);

builder.AddProject<Projects.FluentCMS_App>("web")
    .WithReference(kafka).WaitFor(kafka)
    .WithReference(mongoCmsDb).WaitFor(mongoCmsDb)
    .WithReference(postgresCmsDb).WaitFor(postgresCmsDb);

builder.Build().Run();