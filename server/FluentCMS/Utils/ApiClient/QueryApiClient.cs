using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.ApiClient;

public class QueryApiClient(HttpClient client)
{
    public async Task<Result<QueryResult<Record>>> GetList(string queryName, Cursor cursor, Pagination pagination)
    {
        var url =
            $"/api/queries/{queryName}?first={cursor.First}&last={cursor.Last}&offset={pagination.Offset}&limit={pagination.Limit}";
        return await client.GetObject<QueryResult<Record>>(url);
    }
    public async Task<Result<IDictionary<string,object>>> GetOne(string queryName, object id)
    {
        var url = $"/api/queries/{queryName}/one?id=" + id;
        var (_,_, element,errors) = await client.GetObject<JsonElement>(url);
        if (errors is not null)
        {
            return Result.Fail(errors);
        }
        return element.ToDictionary();
    }
    public async Task<Result<Record[]>> GetMany(string queryName, object[]ids)
    {
        var param = string.Join("&", ids);
        var url = $"/api/queries/{queryName}/many?" + param;
        return await client.GetObject<Record[]>(url);
    }
}