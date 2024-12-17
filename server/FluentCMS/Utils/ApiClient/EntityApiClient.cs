using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.ApiClient;


public class EntityApiClient(HttpClient client)
{
    public Task<Result<ListResponse>> List(
        string entity, int offset, int limit
    ) => client.GetResult<ListResponse>($"/{entity}?offset={offset}&limit={limit}".ToEntityApi());

    public Task<Result<JsonElement>> One(
        string entity, int id
    ) => client.GetResult<JsonElement>($"/{entity}/{id}".ToEntityApi());

    public Task<Result<JsonElement>> Insert(
        string entity, string field, string val
    ) => Insert(entity, new Dictionary<string,object> { { field, val } });

    public Task<Result<JsonElement>> InsertWithLookup(
        string entity, string field, object value, string lookupField, object lookupTargetId
    ) => Insert(entity, new Dictionary<string,object>
    {
        { field, value },
        { lookupField, new { id = lookupTargetId } }
    });

    private Task<Result<JsonElement>> Insert(
        string entity, Dictionary<string,object> payload
    ) => client.PostResult<JsonElement>($"/{entity}/insert".ToEntityApi(), payload);

    public Task<Result> Update(
        string entity, int id, string field, string val
    ) => Update(entity, new Dictionary<string,object>
    {
        { "id", id },
        { field, val }
    });

    private Task<Result> Update(
        string entity, Dictionary<string,object> payload
    ) => client.PostResult($"/{entity}/update".ToEntityApi(), payload);

    public Task<Result> Delete(
        string entity, object payload
    ) => client.PostResult($"/{entity}/delete".ToEntityApi(), payload);

    public Task<Result<ListResponse>> GetJunctionData(
        string source, string target, bool exclude, int sourceId
    ) => client.GetResult<ListResponse>($"/{source}/{sourceId}/{target}?exclude={exclude}".ToEntityApi());

    public Task<Result> JunctionAdd(string entity, string attr, int sourceId, int id)
    {
        var payload = new object[] { new { id } };
        return client.PostResult($"/{entity}/{sourceId}/{attr}/save".ToEntityApi(), payload);
    }

    public Task<Result> JunctionDelete(string entity, string attr, int sourceId, int id)
    {
        var payload = new object[] { new { id } };
        return client.PostResult($"/{entity}/{sourceId}/{attr}/delete".ToEntityApi(), payload);
    }

    public Task<Result<ListResponse>> JunctionList(
        string entity, string attr, int sourceId, bool exclude
    ) => client.GetResult<ListResponse>($"/{entity}/{sourceId}/{attr}?exclude={exclude}".ToEntityApi());

}