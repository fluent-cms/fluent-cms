using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;

namespace FluentCMS.Utils.Feed;

public class Feeder(INosqlDao nosqlDao,ILogger<Feeder> logger, string collectionName, string url)
{
    private readonly HttpClient _client = new();

    public async Task<Result> GetData()
    {
        var viewResult = await Call("");
        while (true)
        {
            if (viewResult.IsFailed)
            {
                return Result.Fail(viewResult.Errors);
            }

            if (!viewResult.Value.HasNext)
            {
                break;
            }

            Thread.Sleep(10);
            viewResult = await Call(viewResult.Value.Last);

        }

        return Result.Ok();

        async Task<Result<ViewResult<JsonElement>>> Call(string last)
        {
            var fullUrl = url;
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
            await nosqlDao.Insert(collectionName ,items);
            return requestResult;
        }
    }
   
}