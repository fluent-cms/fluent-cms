using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.ApiClient;

public class QueryApiClient(HttpClient client)
{
    public async Task<Result<Record[]>> GetList(string queryName, Span span, Pagination pagination)
    {
        var url =
            $"/api/queries/{queryName}?first={span.First}&last={span.Last}&offset={pagination.Offset}&limit={pagination.Limit}";
        return await client.GetObject<Record[]>(url);
    }
    public async Task<Result<Record>> GetOne(string queryName, object id)
    {
        var url = $"/api/queries/{queryName}/one?id=" + id;
        var (_,_, element,errors) = await client.GetObject<JsonElement>(url);
        if (errors is not null)
        {
            return Result.Fail(errors);
        }
        return element.ToDictionary();
    }
    public async Task<Result<Record[]>> GetMany(string queryName, string primaryKey, object[]ids)
    {
        var param = string.Join("&", ids.Select(x=>$"{primaryKey}={x}"));
        var url = $"/api/queries/{queryName}/?" + param;
        return await client.GetObject<Record[]>(url);
    }
}