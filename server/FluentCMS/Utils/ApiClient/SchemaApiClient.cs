using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.RelationDbDao;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.ApiClient;

public class SchemaApiClient (HttpClient client)
{
    public Task<Result<Schema[]>> All(string type) => client.GetResult<Schema[]>($"/?type={type}".ToSchemaApi());

    public Task<Result> Save(Schema schema) => client.PostResult("/".ToSchemaApi(), schema);

    public Task<Result<JsonElement>> One(int id) => client.GetResult<JsonElement>($"/{id}".ToSchemaApi());

    public Task<Result<Schema>> GetTopMenuBar() => client.GetResult<Schema>("/name/top-menu-bar/?type=menu".ToSchemaApi());

    public Task<Result> Delete(int id) => client.DeleteResult($"/{id}".ToSchemaApi());
    
    public Task<Result<Schema>> SaveEntityDefine(Schema schema)
        =>  client.PostResult<Schema>("/entity/define".ToSchemaApi(), schema);

    public Task<Result<Entity>> GetTableDefine(string table)
        =>  client.GetResult<Entity>($"/entity/{table}/define".ToSchemaApi());

    public Task<Result<Entity>> GetLoadedEntity(string entityName)
        => client.GetResult<Entity>($"/entity/{entityName}".ToSchemaApi());

    public async Task<bool> ExistsEntity(string entityName)
    {
        var res = await client.GetAsync($"/name/{entityName}?type=entity".ToSchemaApi());
        return res.IsSuccessStatusCode;
    }
    
    public Task<Result<Schema>> EnsureSimpleEntity(string entityName, string field,
        string lookup = "",
        string junction = "",
        string collection = "", string linkAttribute = "")
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
                DataType: DataType.Lookup,
                DisplayType: DisplayType.Lookup
            ));
        }

        if (!string.IsNullOrWhiteSpace(junction))
        {
            attr.Add(new Attribute
            (
                Field: junction,
                Options: junction,
                Header: junction,
                DataType: DataType.Junction,
                DisplayType: DisplayType.Picklist,
                InDetail: true
            ));
        }

        if (!string.IsNullOrWhiteSpace(collection))
        {
            attr.Add(new Attribute
            (
                Field: collection,
                Options: $"{collection}.{linkAttribute}",
                Header: junction,
                DataType: DataType.Collection,
                DisplayType: DisplayType.EditTable,
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
        return EnsureEntity(entity);
    }

    public Task<Result<Schema>> EnsureEntity(Entity entity)
    {
        var url = $"/entity/add_or_update".ToSchemaApi();
        return client.PostResult<Schema>(url, entity);
    }

    public Task<Result> GraphQlClientUrl() => client.GetResult("/graphql".ToSchemaApi());
}

