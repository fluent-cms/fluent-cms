using System.Text.Json;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.JsonElementExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL;
using GraphQL.Client.Http;

using GraphQL.Client.Serializer.SystemTextJson;

namespace FluentCMS.Utils.ApiClient;

public static class Helper
{
    public static string QueryName(this string entity, bool omitOperationName = false) =>
        omitOperationName ? "" : $"query_{entity}";
}

public class QueryApiClient(HttpClient client)
{
    private readonly GraphQLHttpClient _graph = 
        new ($"{client.BaseAddress!.AbsoluteUri}graphql", new SystemTextJsonSerializer(),client);

    
    // the json decoder will decode unknown object as JsonRecord
    public Task<Result<JsonElement[]>> List(
        string query, string? first = null, string? last = null, StrArgs? args= null, int offset = 0, int limit = 0)
      =>client.GetResult<JsonElement[]>(
              $"/{query}?first={first??""}&last={last??""}&offset={offset}&limit={limit}".ToQueryApi());

    public Task<Result<JsonElement[]>> ListArgs(
        string query, StrArgs args
    ) => client.GetResult<JsonElement[]>($"/{query}?{args.ToQueryString()}".ToQueryApi());
    public Task<Result<JsonElement[]>> Many(
        string query, object[] ids
    ) => client.GetResult<JsonElement[]>(
        $"/{query}/?{string.Join("&", ids.Select(x => $"id={x}"))}".ToQueryApi());

    public Task<Result<JsonElement>> One(
        string query, object id
    ) => client.GetResult<JsonElement>($"/{query}/one?id={id}".ToQueryApi());


    public Task<Result<JsonElement[]>> Part(
        string query, string attr, string? first = null, string? last = null, int limit = 0
    ) => client.GetResult<JsonElement[]>(
        $"/{query}/part/{attr}?first={first ?? ""}&last={last ?? ""}&limit={limit}".ToQueryApi());

    public Task<Result<JsonElement[]>> ListGraphQlByIds(
        string entity, object[] ids, bool omitSave = false, CancellationToken ct = default
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{entity.QueryName(omitSave)}}{
              {{entity}}List(idSet:[{{string.Join(",", ids.Select(x=>x.ToString()))}}])
              { id }
          }
          """, ct);

    public Task<Result<JsonElement[]>> ListGraphQlJunction(
        string entity, string []fields, 
        string junctionField, string[] targetFields,
        bool omitSave = false, CancellationToken ct = default
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{entity.QueryName(omitSave)}}{
            {{entity}}List{
              {{string.Join( "\n", fields)}}
              {{junctionField}}{
                {{string.Join("\n", targetFields)}}
              }
            }
          }
          """, ct);
    
    public Task<Result<JsonElement[]>> ListGraphQl(
        string entity, string[] fields, bool omitSave = false, CancellationToken ct = default
    ) => SendGraphQuery<JsonElement[]>(
        $$"""
          query {{entity.QueryName(omitSave)}}{
            {{entity}}List{
              {{string.Join("\n", fields)}}
            }
          }
          """, ct);

    public Task<Result<JsonElement>> SingleGraphQl(
        string entity,  string[] fields, bool omitSave = false,  CancellationToken ct = default
    ) => SendGraphQuery<JsonElement>(
        $$"""
          query {{entity.QueryName(omitSave)}}{
              {{entity}}{
              {{string.Join("\n", fields)}}
            }
          }
          """, ct);

    private async Task<Result<T>> SendGraphQuery<T>(string query, CancellationToken ct = default)
    {
        var response = await _graph.SendQueryAsync<Dictionary<string,T>>(new GraphQLRequest(query), ct);
        if (response.Errors?.Length > 0)
        {
            return Result.Fail(string.Join(",",response.Errors.Select(x=>x.Message)));
        }
        return response.Data.First().Value ;
    }
}