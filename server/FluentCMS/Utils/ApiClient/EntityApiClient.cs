using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Xunit;

namespace FluentCMS.Utils.ApiClient;

public class EntityApiClient(HttpClient client)
{
    public async Task<Result<ListResult>> GetEntityList(string entity, int offset, int limit)
    {
        return await client.GetObject<ListResult>($"/api/entities/{entity}?offset={offset}&limit={limit}");
    }

    public async Task<Result<Dictionary<string,JsonElement>>> AddDataWithLookup(string entity, string field, object value, string lookupField,
        object lookupTargetId)
    {
        return await AddData(entity, new Dictionary<string, object>
        {
            { field, value },
            { lookupField, new { id = lookupTargetId } }
        });
    }

    public async Task<Result<ListResult>> CrossTable(string source, string target, bool exclude, int sourceId)
    {
        return await client.GetObject<ListResult>($"/api/entities/{source}/{sourceId}/{target}?exclude={exclude}");
    }

    public async Task<Result<Dictionary<string,JsonElement>>> GetEntityValue(string entity, int id)
    {
        return await client.GetObject<Dictionary<string, JsonElement>>($"/api/entities/{entity}/{id}");
    }

    public async Task<Result> AddCrosstableData(string source, string crossField, int sourceId, int targetId)
    {
        var item = new
        {
            id = targetId
        };
        var payload = new object[] { item };
        var res = await client.PostObject($"/api/entities/{source}/{sourceId}/{crossField}/save", payload);
        return await res.ToResult();
    }

    public async Task<Result> UpdateSimpleData(string entity, int id, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { "id", id },
            { field, val }
        };
        return await UpdateData(entity, payload);
    }

    private async Task<Result> UpdateData(string entity, Dictionary<string, object> payload)
    {
        var res = await client.PostObject($"/api/entities/{entity}/update", payload);
        return await res.ToResult();
    }

    public async Task<Result<Dictionary<string,JsonElement>>> AddSimpleData(string entity, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { field, val }
        };
        return await AddData(entity, payload);
    }

    private async Task<Result<Dictionary<string,JsonElement>>> AddData(string entity, Dictionary<string, object> payload)
    {
        return await client.PostObject<Dictionary<string,JsonElement>>($"/api/entities/{entity}/insert", payload);
    }
}