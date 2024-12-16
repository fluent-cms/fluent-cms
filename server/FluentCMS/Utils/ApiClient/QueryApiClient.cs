using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL;
using GraphQL.Client.Http;

using GraphQL.Client.Serializer.SystemTextJson;

namespace FluentCMS.Utils.ApiClient;

public class QueryApiClient(HttpClient client)
{
    private readonly GraphQLHttpClient _graph = new ($"{client.BaseAddress!.AbsoluteUri}graphql", new SystemTextJsonSerializer(),client);

    public  Task<Result<JsonElement>> SendSingleGraphQuery(string entity, string[]fields, bool withName = false, CancellationToken ct = default)
    {
        var operationName = withName ? entity : "";
        return SendGraphQuery(
           $$"""
           query {{operationName}}{
               {{entity}}{
               {{string.Join("\n",fields)}}
             }
           }
           """, 
           ct);
    }

    private async Task<Result<JsonElement>> SendGraphQuery(string query, CancellationToken ct = default)
    {
        var response = await _graph.SendQueryAsync<JsonElement>(new GraphQLRequest(query), ct);
        if (response.Errors?.Length > 0)
        {
            return Result.Fail(string.Join(",",response.Errors.Select(x=>x.Message)));
        }
        return response.Data;
    }
    public async Task<Result<Record[]>> GetList(string queryName, Span span, Pagination pagination)
    {
        var url =
            $"/api/queries/{queryName}?first={span.First}&last={span.Last}&offset={pagination.Offset}&limit={pagination.Limit}";
        return await client.GetResult<Record[]>(url);
    }
    public async Task<Result<Record>> GetOne(string queryName, object id)
    {
        var url = $"/api/queries/{queryName}/one?id=" + id;
        var (_,_, element,errors) = await client.GetResult<JsonElement>(url);
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
        return await client.GetResult<Record[]>(url);
    }
}