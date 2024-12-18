using FluentCMS.Utils.DataFetch;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Consumers;

public record ApiLinks(string Entity, string Api, string Collection);
public sealed class DocDbLinker(
    IConsumer consumer, 
    DataFetcher dataFetcher, 
    IDocumentDbDao dao,
    ILogger<DocDbLinker> logger,
    ApiLinks[]links): BackgroundService
{
    private readonly Dictionary<string, ApiLinks> _dict = links.ToDictionary(x => x.Entity, x => x);
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

                if (!_dict.TryGetValue(message.EntityName, out var apiLinks))
                {
                    logger.LogWarning("entity [{message.EntityName}] is not in Feed Dictionary, ignore the message", message.EntityName);
                    continue;
                    
                }
                switch (message.Operation)
                {
                    case Operations.Create:
                    case Operations.Update:
                        if (!(await dataFetcher.FetchSaveSingle(apiLinks.Api, apiLinks.Collection, message.Id)).Try(
                                out var err))
                        {
                            logger.LogWarning("failed to fetch and save single item, err ={err}", err);
                        }
                        break;
                    case Operations.Delete:
                        await dao.Delete(apiLinks.Collection, message.Id);
                        break;
                    default:
                        logger.LogWarning("unknown operation {message.Operation}, ignore the message",message.Operation);
                        continue;
                }

                logger.LogInformation(
                    "consumed message successfully, entity={message.EntityName}, operation={message.Operation}, id = {message.Id}",
                    message.EntityName, message.Operation, message.Id);
            }
            catch (Exception e)
            {
                if (e != null)
                {
                    logger.LogError("Exception when try to sync data, error message = {e.Message}",e.Message);
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