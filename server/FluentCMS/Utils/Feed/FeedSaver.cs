using System.Text.Json;
using Amazon.Runtime.Internal;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.Feed;

public record FeedConfig(string CollectionName, string Url);
public class FeedSaver(INosqlDao nosqlDao,ILogger<FeedSaver> logger)
{
    private readonly HttpClient _client = new();


    public async Task GetData(FeedConfig config,string id)
    {
        var result = await Call();
        if (result.IsFailed)
        {
            logger.LogError(string.Join("\r\n", result.Errors));
        }
        
        return;
        async Task<Result> Call()
        {
            var fullUrl = $"{config.Url}?id={id}";
            var requestResult = await _client.GetObject<JsonElement>(fullUrl);
            if (requestResult.IsFailed)
            {
                return Result.Fail(requestResult.Errors);
            }

            var item = requestResult.Value.ToDictionary();
            try
            {
                await nosqlDao.Insert(config.CollectionName, [item]);
                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }
    }
    public async Task BatchSaveData(FeedConfig config)
    {
        var viewResult = await Call("");
        while (true)
        {
            if (viewResult.IsFailed)
            {
                logger.LogError(string.Join("\r\n",viewResult.Errors));
            }

            if (!viewResult.Value.HasNext)
            {
                break;
            }

            Thread.Sleep(10);
            viewResult = await Call(viewResult.Value.Last);

        }

        return;

        async Task<Result<ViewResult<JsonElement>>> Call(string last)
        {
            var fullUrl = config.Url;
            if (!string.IsNullOrWhiteSpace(last))
            {
                fullUrl += $"?last={last}";
            }

            var requestResult = await _client.GetObject<ViewResult<JsonElement>>(fullUrl);
            if (requestResult.IsFailed)
            {
                return Result.Fail(requestResult.Errors);
            }

            logger.LogInformation($"succeed to download feed from {fullUrl}");

            var items = requestResult.Value.Items!.Select(x => x.ToDictionary());
            try
            {
                await nosqlDao.Insert(config.CollectionName ,items);
                return requestResult;
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }
    }
   
}