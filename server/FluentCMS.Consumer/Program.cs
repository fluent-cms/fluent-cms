using FluentCMS.Utils.Feed;
using FluentCMS.Utils.Nosql;
using FluentCMS.HostAppExt;
using FluentCMS.Utils.EventStreaming;

var builder = Host.CreateApplicationBuilder(args);
var mongoConfig = builder.Configuration.GetRequiredSection("MongoConfig").Get<MongoConfig>()!;
var kafkaConfig = builder.Configuration.GetRequiredSection("KafkaConfig").Get<KafkaConfig>()!;
var entityToFeed = builder.Configuration.GetRequiredSection("EntityToFeed").Get<IDictionary<string,FeedConfig>>()!;
builder.AddMongoCmsConsumer(mongoConfig, kafkaConfig, entityToFeed);

var host = builder.Build();
host.Run();