using FluentCMS.Cms.Models;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using Xunit;

namespace FluentCMS.Utils.ApiClient;

public class SchemaApiClient (HttpClient client) 
{
    public async Task DeleteSchema(int id)
    {
        var res = await client.DeleteAsync($"/api/schemas/{id}");
        res.EnsureSuccessStatusCode();
    }
    public async Task<Schema> SaveEntityDefine(Schema schema)
    {
        var (success, _, ret) = await client.PostObject<Schema>("/api/schemas/entity/define", schema);
        Assert.True(success);
        Assert.NotNull(ret);
        return ret;
    }

    public async Task GetLoadedEntityFail(string entityName)
    {
        var (_, fail, _) = await client.GetObject<Entity>($"/api/schemas/entity/{entityName}");
        Assert.True(fail);
    }
    public async Task<Entity> GetLoadedEntitySucceed(string entityName)
    {
        var (success, _, entity) = await client.GetObject<Entity>($"/api/schemas/entity/{entityName}");
        Assert.True(success);
        Assert.Equal(entityName, entity.Name);
        return entity;
    }

    public async Task AddSimpleEntity(string entity, string field)
    {
        await AddSimpleEntity(entity, field, "", "");
    }

    public async Task AddSimpleEntity(string entity, string field, string lookup, string crosstable)
    {
        var result =
            await client.PostObject(
                $"/api/schemas/simple_entity_define?entity={entity}&field={field}&lookup={lookup}&crosstable={crosstable}",
                new Dictionary<string, object>());
        result.EnsureSuccessStatusCode();
    }

    public async Task GetTopMenuBar()
    {
        var response = await client.GetAsync("/api/schemas/name/top-menu-bar/?type=menu");
        response.EnsureSuccessStatusCode();
    }

    public async Task<Schema[]> GetAll(string type)
    {
        var (success,_,schemas) = await client.GetObject<Schema[]>($"/api/schemas?type={type}");
        Assert.True(success);
        return schemas;
    }
}