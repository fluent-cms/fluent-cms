using FluentCMS.Cms.Models;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Xunit;

namespace FluentCMS.Utils.ApiClient;

public class SchemaApiClient (HttpClient client) 
{
    public async Task<Result> DeleteSchema(int id)
    {
        var url = $"/api/schemas/{id}";
        var res = await client.DeleteAsync(url);
        return await res.ToResult();
    }
    
    public async Task<Result<Schema>> SaveEntityDefine(Schema schema)
    {
        return await client.PostObject<Schema>("/api/schemas/entity/define", schema);
    }

    public async Task<Result<Entity>> GetLoadedEntity(string entityName)
    {
        return await client.GetObject<Entity>($"/api/schemas/entity/{entityName}");
    }
    
    public async Task<Result> AddSimpleEntity(string entity, string field)
    {
        return await AddSimpleEntity(entity, field, "", "");
    }

    public async Task<Result> AddSimpleEntity(string entity, string field, string lookup, string crosstable)
    {
        var url =
            $"/api/schemas/simple_entity_define?entity={entity}&field={field}&lookup={lookup}&crosstable={crosstable}"; 
        var res = await client.PostObject(url , new Dictionary<string, object>());
        return await res.ToResult();
    }

    public async Task<Result> GetTopMenuBar()
    {
        var url = "/api/schemas/name/top-menu-bar/?type=menu";
        var res = await client.GetAsync(url);
        return await res.ToResult();
    }

    public async Task<Result<Schema[]>> GetAll(string type)
    {
        return await client.GetObject<Schema[]>($"/api/schemas?type={type}");
    }
}

