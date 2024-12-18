using FluentCMS.Consumers;
using FluentCMS.HostAppBuilders;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.EventStreaming;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Mongo")!;
var kafkaConfig = builder.Configuration.GetRequiredSection("KafkaConfig").Get<KafkaConfig>()!;
var apiLinksArray = builder.Configuration.GetRequiredSection("ApiLinksArray").Get<ApiLinks[]>()!;
builder.AddMongoCmsConsumer(kafkaConfig, new MongoDaoConfig(connectionString), apiLinksArray);

var host = builder.Build();
host.Run();