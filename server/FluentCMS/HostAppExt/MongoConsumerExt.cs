using FluentCMS.BackgroundServices;
using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.Feed;
using FluentCMS.Utils.Nosql;

namespace FluentCMS.HostAppExt;


public static class MongoConsumerExt
{
    public static void AddMongoCmsConsumer(
        this HostApplicationBuilder builder,
        MongoConfig mongoConfig,
        KafkaConfig kafkaConfig,
        IDictionary<string, FeedConfig> dictionary
    )
    {
        builder.Services.AddSingleton<INosqlDao>(p => new MongoNosqlDao(mongoConfig));
        builder.Services.AddSingleton<FeedSaver>();
        builder.Services.AddSingleton<IConsumer>(p => new KafkaConsumer(kafkaConfig));
        builder.Services.AddHostedService<NosqlConsumerService>(p =>
            new NosqlConsumerService(
                p.GetService<IConsumer>()!,
                p.GetService<FeedSaver>()!,
                p.GetService<INosqlDao>()!,
                dictionary,
                p.GetService<ILogger<NosqlConsumerService>>()!
            )
        );
    }

    public static async Task BatchLoadFeed(this IHost host, FeedConfig config)
    {
        var dao = host.Services.GetService<INosqlDao>()!;
        var logger = host.Services.GetService<ILogger<FeedSaver>>()!;
        var feed = new FeedSaver(dao, logger);
        await feed.BatchSaveData(config);
    }
}