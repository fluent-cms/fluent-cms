using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.Feed;
using FluentCMS.Utils.Nosql;

namespace FluentCMS.BackgroundServices;

public class NosqlConsumerService(
    IConsumer consumer, 
    FeedSaver feedSaver, 
    INosqlDao nosqlDao,
    IDictionary<string,FeedConfig> dictEntityFeed,
    ILogger<NosqlConsumerService> logger): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe();
        logger.LogInformation("background service started...");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await consumer.Consume(stoppingToken);
                if (message is null)
                {
                    logger.LogWarning("got empty message, ignore the message");
                    continue;
                }

                if (!dictEntityFeed.TryGetValue(message.EntityName, out var config))
                {
                    logger.LogWarning($"entity {message.EntityName} is not in Feed Dictionary, ignore the message");
                    continue;
                    
                }
                switch (message.Operation)
                {
                    case Operations.Create:
                    case Operations.Update:
                        await feedSaver.SaveData(config, message.Id);
                        break;
                    case Operations.Delete:
                        await nosqlDao.Delete(config.CollectionName, message.Id);
                        break;
                    default:
                        logger.LogWarning($"unknown operation {message.Operation}, ignore the message");
                        continue;
                }
                logger.LogInformation($"consumed message successfully, entity={message.EntityName}, operation={message.Operation}, id = {message.Id}");
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