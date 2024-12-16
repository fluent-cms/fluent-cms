using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.ApiClient;
using JsonItem = Dictionary<string, JsonElement>;
using Item = Dictionary<string, object>;

public class EntityApiClient(HttpClient client)
{
    public  Task<Result<ListResult>> List(
        string entity, int offset, int limit
    ) =>  client.GetResult<ListResult>($"/{entity}?offset={offset}&limit={limit}".ToEntityApi());

    public Task<Result<JsonItem>> One(
        string entity, int id
    ) => client.GetResult<JsonItem>($"/{entity}/{id}".ToEntityApi());

    public  Task<Result<JsonItem>> Insert(
        string entity, string field, string val
    ) => Insert(entity, new Item { { field, val } });

    public Task<Result<JsonItem>> InsertWithLookup(
        string entity, string field, object value, string lookupField, object lookupTargetId
    ) => Insert(entity, new Item
    {
        { field, value },
        { lookupField, new { id = lookupTargetId } }
    });

    private Task<Result<JsonItem>> Insert(
        string entity, Item payload
    ) => client.PostResult<JsonItem>($"/{entity}/insert".ToEntityApi(), payload);

    public  Task<Result> Update(
        string entity, int id, string field, string val
    ) =>  Update(entity, new Item
    {
        { "id", id },
        { field, val }
    });

    private Task<Result> Update( 
        string entity, Item payload
    ) => client.PostResult($"/{entity}/update".ToEntityApi(), payload);

    public Task<Result> Delete(
        string entity, object payload
    ) => client.PostResult($"/{entity}/delete".ToEntityApi(), payload);

    public  Task<Result<ListResult>> GetJunctionData(
        string source, string target, bool exclude, int sourceId
    ) => client.GetResult<ListResult>($"/{source}/{sourceId}/{target}?exclude={exclude}".ToEntityApi());


    public Task<Result> AddJunctionData(string source, string crossField, int sourceId, int targetId)
    {
        var payload = new object[] { new
        {
            id = targetId
        } };
        return client.PostResult($"/api/entities/{source}/{sourceId}/{crossField}/save", payload);
    }
}