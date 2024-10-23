using FluentCMS.Utils.HttpClientExt;
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
    public async Task<Result<Record>> GetOne(string queryName, object id)
    {
        var url = $"/api/queries/{queryName}/one?id=" + id;
        return await client.GetObject<Record>(url);
    }
    public async Task<Result<Record[]>> GetMany(string queryName, object[]ids)
    {
        var param = string.Join("&", ids);
        var url = $"/api/queries/{queryName}/many?" + param;
        return await client.GetObject<Record[]>(url);
    }
}