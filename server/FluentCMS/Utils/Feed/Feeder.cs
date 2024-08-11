using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.Nosql;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;

namespace FluentCMS.Utils.Feed;

public class Feeder(IDao dao,ILogger<Feeder> logger, string collectionName, string url)
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

        async Task<Result<ViewResult>> Call(string last)
        {
            var fullUrl = url;
            if (!string.IsNullOrWhiteSpace(last))
            {
                fullUrl += $"?last={last}";
            }

            var requestResult = await _client.GetObject<ViewResult>(fullUrl);
            if (requestResult.IsFailed)
            {
                return Result.Fail(requestResult.Errors);
            }

            logger.LogInformation($"succeed to download feed from {fullUrl}");

            var items = requestResult.Value.Items!.Select(x => ConvertJsonElements(x)).ToArray();
            await dao.Insert(collectionName ,items);
            return requestResult;
        }
    }
    static Record ConvertJsonElements(Record original)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in original)
        {
            result[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        return result;
    }

    static object ConvertJsonElement(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Object => ConvertJsonElementToDictionary(jsonElement),
                JsonValueKind.Array => ConvertJsonElementToArray(jsonElement),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetInt32(out int i) ? (object)i : jsonElement.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value
            };
        }

        return value;
    }

    static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement jsonElement)
    {
        var dict = new Dictionary<string, object>();

        foreach (var prop in jsonElement.EnumerateObject())
        {
            dict[prop.Name] = ConvertJsonElement(prop.Value);
        }

        return dict;
    }

    static List<object> ConvertJsonElementToArray(JsonElement jsonElement)
    {
        var list = new List<object>();

        foreach (var item in jsonElement.EnumerateArray())
        {
            list.Add(ConvertJsonElement(item));
        }

        return list;
    }
}