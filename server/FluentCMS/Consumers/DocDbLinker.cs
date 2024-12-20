using System.Text.Json;
using FluentCMS.Utils.DataFetch;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.EventStreaming;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Consumers;

public record ApiLinks(string Entity, string Api, string Collection);
public sealed class DocDbLinker(
    IStringMessageConsumer consumer, 
    DataFetcher dataFetcher, 
    IDocumentDbDao dao,
    ILogger<DocDbLinker> logger,
    ApiLinks[]links): BackgroundService
{
    private readonly Dictionary<string, ApiLinks> _dict = links.ToDictionary(x => x.Entity, x => x);
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(async s =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<RecordMessage>(s);
                if (message is null)
                {
                    logger.LogWarning("Could not deserialize message");
                    return;
                }
                
                if (!_dict.TryGetValue(message.EntityName, out var apiLinks))
                {
                    logger.LogWarning("entity [{message.EntityName}] is not in Feed Dictionary, ignore the message", message.EntityName);
                }
                
                switch (message.Operation)
                {
                    case Operations.Create:
                    case Operations.Update:
                        if (!(await dataFetcher.FetchSaveSingle(apiLinks!.Api, apiLinks.Collection, message.Id)).Try(
                                out var err))
                        {
                            logger.LogWarning("failed to fetch and save single item, err ={err}", err);
                        }
                        break;
                    case Operations.Delete:
                        await dao.Delete(apiLinks!.Collection, message.Id);
                        break;
                    default:
                        logger.LogWarning("unknown operation {message.Operation}, ignore the message",message.Operation);
                        break;  
                }

                logger.LogInformation(
                    "consumed message successfully, entity={message.EntityName}, operation={message.Operation}, id = {message.Id}",
                    message.EntityName, message.Operation, message.Id);
            }
            catch (Exception e)
            {
                logger.LogError("Fail to handler message, err= {error}", e.Message);
            }
            
        },ct);
    }
}