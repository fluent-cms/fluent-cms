using System.Text.Json;
using FluentCMS.Utils.DocumentDb;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Utils.DataFetch;

public class DataFetcher(IDocumentDbDao dao, ILogger<DataFetcher> logger)
{
    private readonly HttpClient _client = new();

    public async Task<Result> FetchSaveSingle(
        string api, string collection, string id
    ) => (await _client.GetResult<JsonElement>($"{api}?id={id}")).Try(out var ele, out var err)
        ? await Result.Try(() => dao.Upsert(collection, id, ele.ToDictionary()))
        : Result.Fail(err).WithError("Failed to fetch single data");

    public async Task BatchSaveData(string api, string collection)
    {
        var res = await FetchAndSave("");
        while (res is { IsSuccess: true, Value.items.Length: > 0 }) 
        {
            logger.LogInformation($"succeed to download data from {api}, last = {res.Value.lastCursor}");
            if (!SpanHelper.HasNext(res.Value.items))
            {
                break;
            }
            res = await FetchAndSave(SpanHelper.LastCursor(res.Value.items));
        }

        if (res.IsFailed)
        {
            var msg = string.Join(",", res.Errors.Select(x => x.Message));
            logger.LogError("Failed to execute Batch save data, err={msg}",msg);
        }
        else
        {
            logger.LogInformation("Finished executing batch save");
        }
        return;

        async Task<Result<(string lastCursor,Record[] items)>> FetchAndSave(string last = "")
        {
            var url = api + $"?last={last ?? ""}";
            if (!(await _client.GetResult<JsonElement[]>(url)).Try(out var arr, out var err))
            {
                return Result.Fail(err).WithError("Failed to fetch data");
            }

            var items = arr.Select(Record (x) => x.ToDictionary()).ToArray();
            try
            {
                await dao.BatchInsert(collection, items);
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }

            return (last??"",items);
        }
    }
}