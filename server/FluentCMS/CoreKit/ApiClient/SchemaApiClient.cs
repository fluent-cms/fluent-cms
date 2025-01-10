using System.Text.Json;
using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.HttpClientExt;
using FluentResults;
using QueryBuilder_Attribute = FluentCMS.Core.Descriptors.Attribute;

namespace FluentCMS.CoreKit.ApiClient;

public class SchemaApiClient (HttpClient client)
{
    public Task<Result<Schema[]>> All(SchemaType? type) => client.GetResult<Schema[]>($"/?type={type?.ToCamelCase()}".ToSchemaApi(),JsonOptions.IgnoreCase);

    public Task<Result> Save(Schema schema) => client.PostResult("/".ToSchemaApi(), schema,JsonOptions.IgnoreCase);

    public Task<Result<JsonElement>> One(int id) => client.GetResult<JsonElement>($"/{id}".ToSchemaApi(),JsonOptions.IgnoreCase);

    public Task<Result<Schema>> GetTopMenuBar() => client.GetResult<Schema>("/name/top-menu-bar/?type=menu".ToSchemaApi(),JsonOptions.IgnoreCase);

    public Task<Result> Delete(int id) => client.DeleteResult($"/{id}".ToSchemaApi());
    
    public Task<Result<Schema>> SaveEntityDefine(Schema schema)
        =>  client.PostResult<Schema>("/entity/define".ToSchemaApi(), schema,JsonOptions.IgnoreCase);

    public Task<Result<Entity>> GetTableDefine(string table)
        =>  client.GetResult<Entity>($"/entity/{table}/define".ToSchemaApi(),JsonOptions.IgnoreCase);

    public Task<Result<Entity>> GetLoadedEntity(string entityName)
        => client.GetResult<Entity>($"/entity/{entityName}".ToSchemaApi(),JsonOptions.IgnoreCase);

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
        var attr = new List<QueryBuilder_Attribute>([
            new QueryBuilder_Attribute
            (
                Field: field,
                Header: field
            )

        ]);
        if (!string.IsNullOrWhiteSpace(lookup))
        {
            attr.Add(new QueryBuilder_Attribute
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
            attr.Add(new QueryBuilder_Attribute
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
            attr.Add(new QueryBuilder_Attribute
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
        return client.PostResult<Schema>(url, entity,JsonOptions.IgnoreCase);
    }

    public Task<Result> GraphQlClientUrl() => client.GetResult("/graphql".ToSchemaApi());
}

