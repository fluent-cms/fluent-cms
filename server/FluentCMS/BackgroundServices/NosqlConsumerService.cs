using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.Feed;

namespace FluentCMS.BackgroundServices;

public class NosqlConsumerService(
    IConsumer consumer, 
    FeedSaver feedSaver, 
    IDictionary<string,FeedConfig> dictEntityFeed,
    ILogger<NosqlConsumerService> logger): BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        consumer.Subscribe();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (topic, message) = consumer.Consume(stoppingToken);
                if (message is null)
                {
                    continue;
                }
                switch (topic)
                {
                    case Topics.EntityCreated:
                        if (dictEntityFeed.TryGetValue(message.EntityName, out var config))
                        {
                            await feedSaver.GetData(config, message.Id);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                if (e != null)
                {
                    logger.LogError(e.Message);
                }
            }
            
        }
    }

    public override void Dispose()
    {
        consumer.Dispose();
        base.Dispose();
    }
}