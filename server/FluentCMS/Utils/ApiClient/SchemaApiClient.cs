using FluentCMS.Cms.Models;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.ApiClient;

public class SchemaApiClient (HttpClient client) 
{
    public async Task<Result<Schema[]>> GetAll(string type)
    {
        return await client.GetResult<Schema[]>($"/api/schemas?type={type}");
    }

    public  Task<Result> SaveSchema(Schema schema) =>
         client.PostResult("/api/schemas", schema);

    public async Task<Result> DeleteSchema(int id)
    {
        var url = $"/api/schemas/{id}";
        var res = await client.DeleteAsync(url);
        return await res.GetResult();
    }

    public async Task<Result> GetTopMenuBar()
    {
        var url = "/api/schemas/name/top-menu-bar/?type=menu";
        var res = await client.GetAsync(url);
        return await res.GetResult();
    }


    public async Task<Result<Schema>> SaveEntityDefine(Schema schema)
    {
        return await client.PostResult<Schema>("/api/schemas/entity/define", schema);
    }

    public async Task<Result<Entity>> GetLoadedEntity(string entityName)
    {
        return await client.GetResult<Entity>($"/api/schemas/entity/{entityName}");
    }
    
    public Task<Result<Schema>> EnsureSimpleEntity(string entity, string field) =>
        EnsureSimpleEntity(entity, field, "", "");

    public async Task<Result<Schema>> EnsureSimpleEntity(string entityName, string field, string lookup, string junction)
    {
        var attr = new List<Attribute>([
            new Attribute
            (
                Field: field,
                Header: field
            )

        ]);
        if (!string.IsNullOrWhiteSpace(lookup))
        {
            attr.Add(new Attribute
            (
                Field: lookup,
                Options: lookup,
                Header: lookup,
                InList: true,
                InDetail: true,
                DataType: DataType.Int,
                Type: DisplayType.Lookup
            ));
        }

        if (!string.IsNullOrWhiteSpace(junction))
        {
            attr.Add(new Attribute
            (
                Field: junction,
                Options: junction,
                Header: junction,
                DataType: DataType.Na,
                Type: DisplayType.Junction,
                InDetail: true
            ));
        }

        var entity = new Entity
        (
            Name: entityName,
            TableName: entityName,
            Title: entityName,
            DefaultPageSize: EntityConstants.DefaultPageSize,
            PrimaryKey: "id",
            TitleAttribute: field,
            Attributes: [..attr]
        );

        var url =
            $"/api/schemas/entity/add_or_update";
        return await client.PostResult<Schema>(url, entity);
    }




}

