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
        return await client.GetObject<Schema[]>($"/api/schemas?type={type}");
    }

    public async Task<Result> SaveSchema(Schema schema)
    {
        return await (await client.PostObject("/api/schemas", schema)).ToResult();
    }

    public async Task<Result> DeleteSchema(int id)
    {
        var url = $"/api/schemas/{id}";
        var res = await client.DeleteAsync(url);
        return await res.ToResult();
    }

    public async Task<Result> GetTopMenuBar()
    {
        var url = "/api/schemas/name/top-menu-bar/?type=menu";
        var res = await client.GetAsync(url);
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
    
    public Task<Result<Schema>> EnsureSimpleEntity(string entity, string field) =>
        EnsureSimpleEntity(entity, field, "", "");

    public async Task<Result<Schema>> EnsureSimpleEntity(string entityName, string field, string lookup, string crosstable)
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

        if (!string.IsNullOrWhiteSpace(crosstable))
        {
            attr.Add(new Attribute
            (
                Field: crosstable,
                Options: crosstable,
                Header: crosstable,
                DataType: DataType.Na,
                Type: DisplayType.Crosstable,
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
        return await client.PostObject<Schema>(url, entity);
    }




}

