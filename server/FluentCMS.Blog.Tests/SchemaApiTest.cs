using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.ApiClient;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.IntegrationTests;
[Collection("Sequential")]

public class SchemaApiTest
{
    private readonly SchemaApiClient _schemaApiClient;

    public SchemaApiTest()
    {
        WebAppClient<Program> webAppClient = new();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
    }
    
    [Fact]
    public async Task SaveSchema()
    {
        var schema = TestSchema();
        await _schemaApiClient.SaveEntityDefine(schema);
        await _schemaApiClient.GetLoadedEntitySucceed(schema.Name);
    }

    [Fact]
    public async Task SaveSchemaTwice()
    {
        var schema = TestSchema();
        var res = await _schemaApiClient.SaveEntityDefine(schema);
        await _schemaApiClient. SaveEntityDefine(res);
    }
    
    [Fact]
    public async Task SaveSchema_Update()
    {
        var schema = TestSchema();
        schema = await _schemaApiClient.SaveEntityDefine(schema);
        schema.Settings.Entity = schema.Settings.Entity! with { DefaultPageSize = 10 };
        await _schemaApiClient.SaveEntityDefine(schema);
        var entity = await _schemaApiClient.GetLoadedEntitySucceed(schema.Name);
        Assert.Equal(10,entity.DefaultPageSize);
    }

    [Fact]
    public async Task Delete_Success()
    {
        var schema = TestSchema();
        schema = await _schemaApiClient.SaveEntityDefine(schema);
        await _schemaApiClient.DeleteSchema(schema.Id);
        await _schemaApiClient.GetLoadedEntityFail(schema.Name);
   }

    [Fact]
    public async Task GetAll_NUllType()
    {
        var items = await _schemaApiClient.GetAll("");
        var len = items.Length;
        var schema = TestSchema();
        await _schemaApiClient.SaveEntityDefine(schema);
        items = await _schemaApiClient.GetAll("");
        Assert.Equal(len + 1, items.Length );
    }

    [Fact]
    public async Task GetAll_EntityType()
    {
        var items = await _schemaApiClient.GetAll(SchemaType.Menu);
        Assert.Single(items);
    }


    private static Entity RandomTestEntity() => new 
    (
        Name: $"IntegrationTest{DateTime.Now.Millisecond}",
        PrimaryKey: "id",
        TableName: $"integration_test{DateTime.Now.Millisecond}",
        TitleAttribute: "Title",
        Attributes:
        [
            new Attribute
            (
                Field: "Title",
                DataType: DataType.String
            )
        ]
    );

    private static Schema TestSchema() => new Schema
    {
        Name = RandomTestEntity().Name,
        Type = SchemaType.Entity,
        Settings = new Settings
        {
            Entity = RandomTestEntity()
        }
    };
}