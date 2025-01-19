using System.Text.Json;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Utils.EnumExt;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.CoreKit.ApiClient;

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
    
    public Task<Result<Schema>> EnsureSimpleEntity(string entityName, string field, bool needPublish,
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
            DisplayName: entityName,
            DefaultPageSize: EntityConstants.DefaultPageSize,
            PrimaryKey: "id",
            LabelAttributeName: field,
            DefaultPublicationStatus: needPublish ?PublicationStatus.Draft:PublicationStatus.Published,
            Attributes: [..attr]
        );
        return EnsureEntity(entity);
    }

    public Task<Result<Schema>> EnsureEntity(string entityName, string labelAttribute, bool needPublish,params Attribute[] attributes)
    {
        var entity = new Entity(
            PrimaryKey: DefaultAttributeNames.Id.ToCamelCase(),
            Attributes:[..attributes],
            Name: entityName,
            TableName: entityName,
            DisplayName: entityName,
            LabelAttributeName: labelAttribute,
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus: needPublish ?PublicationStatus.Draft:PublicationStatus.Published
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
