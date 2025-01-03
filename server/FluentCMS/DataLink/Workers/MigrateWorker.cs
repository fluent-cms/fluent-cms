using System.Text.Json;
using FluentCMS.DataLink.Types;
using FluentCMS.Utils.DocumentDbDao;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.DataLink.Workers;


public class MigrateWorker( IDocumentDbDao dao,
    ILogger<SyncWorker> logger,
    ApiLinks[] linksArray):   BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string coll = "Progress";
        while (stoppingToken.IsCancellationRequested == false)
        {
            var pg = new ValidPagination(0, 1000);
            if (!(await dao.Query(coll, [], [], pg)) .Try(out var progresses, out var err))
            {
                logger.LogError("Fail to get progress data, err={err}", err?.Select(x => x.Message));
            }

            foreach (var link in linksArray)
            {
                if (progresses.FirstOrDefault(x => x["collection"] is string s && s == link.Collection) is not null)
                {
                    continue;
                }
                await BatchSaveData(link);
                await dao.Upsert(coll, "collection", link.Collection, new { collection = link.Collection });
            }

            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }

    private async Task BatchSaveData(ApiLinks links)
    {
        var res = await FetchAndSave(links,"");
        while (res.IsSuccess)
        {
            var (curr, next) = res.Value;
            logger.LogInformation("succeed to download data from links={links}, cursor = {curr}", links, curr);
            if (next == "")
            {
                break;
            }
            res = await FetchAndSave(links, next);
        }
        if (res.IsFailed)
        {
            var msg = string.Join(",", res.Errors.Select(x => x.Message));
            logger.LogError("Failed to execute Batch save data, links ={},err={msg}", links,msg);
        }

        logger.LogInformation("Finished executing batch save for {links}",links);
    }

    private async Task<Result<(string curosr,string next)>> FetchAndSave(ApiLinks links,string cursor)
    {
        var url = links.Api + $"?last={cursor}";
        if (!(await new HttpClient().GetStringResult(url)).Try(out var s, out var err))
        {
            return Result.Fail(err).WithError("Failed to fetch data");
        }
        
        var elements = JsonSerializer.Deserialize<JsonElement[]>(s);
        if (elements is null || elements.Length == 0)
        {
            return (cursor,"");
        }

        var items = elements.Select(x => x.ToDictionary()).ToArray();
        var nextCursor = SpanHelper.HasNext(items) ? SpanHelper.LastCursor(items) : "";
        
        foreach (var item in items)
        {
            SpanHelper.Clean(item);
        }
        try
        {
            await dao.BatchInsert(links.Collection, items);
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
        return (cursor,nextCursor);
    }
}