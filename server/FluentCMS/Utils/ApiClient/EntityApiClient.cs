using System.Text.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using Xunit;

namespace FluentCMS.Utils.ApiClient;

public class EntityApiClient(HttpClient client)
{
    public async Task GetEntityList(string entity, int offset, int limit, int shouldTotal, int itemCount)
    {
        var (_, _, res) = await client.GetObject<ListResult>($"/api/entities/{entity}?offset={offset}&limit={limit}");
        Assert.Equal(shouldTotal, res.TotalRecords);
        Assert.Equal(itemCount, res.Items.Length);
    }

    public async Task AddDataWithLookup(string entity, string field, object value, string lookupField,
        object lookupTargetId)
    {
        await AddData(entity, new Dictionary<string, object>
        {
            { field, value },
            { lookupField, new { id = lookupTargetId } }
        });
    }

    public async Task CrossTableCount(string source, string target, bool exclude, int sourceId, int count)
    {
        var (_, _, res) =
            await client.GetObject<ListResult>($"/api/entities/{source}/{sourceId}/{target}?exclude={exclude}");
        Assert.Equal(count, res.TotalRecords);
    }

    public async Task GetEntityValue(string entity, int id, string field, string value)
    {
        var (_, _, item) = await client.GetObject<Dictionary<string, JsonElement>>($"/api/entities/{entity}/{id}");
        Assert.Equal("TomUpdate", item[field].GetString());
    }

    public async Task AddCrosstableData(string source, string crossField, int sourceId, int targetId)
    {
        var item = new
        {
            id = targetId
        };
        var payload = new object[] { item };
        var res = await client.PostObject($"/api/entities/{source}/{sourceId}/{crossField}/save", payload);
        res.EnsureSuccessStatusCode();

    }

    public async Task UpdateSimpleData(string entity, int id, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { "id", id },
            { field, val }
        };
        await UpdateData(entity, payload);
    }

    private async Task UpdateData(string entity, Dictionary<string, object> payload)
    {
        var res = await client.PostObject($"/api/entities/{entity}/update", payload);
        res.EnsureSuccessStatusCode();
    }

    public async Task AddSimpleData(string entity, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { field, val }
        };
        await AddData(entity, payload);
    }

    private async Task AddData(string entity, Dictionary<string, object> payload)
    {
        var res = await client.PostObject($"/api/entities/{entity}/insert", payload);
        res.EnsureSuccessStatusCode();
    }
}