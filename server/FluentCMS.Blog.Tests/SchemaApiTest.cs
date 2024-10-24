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
    private readonly AccountApiClient _accountApiClient;


    public SchemaApiTest()
    {
        WebAppClient<Program> webAppClient = new();
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task AnonymousGetAllSchema()
    {
        var schema = TestSchema();
        (await _schemaApiClient.GetAll("")).AssertFail();
    }


    [Fact]
    public async Task AnonymousSaveSchemaFail()
    {
        var schema = TestSchema();
        (await _schemaApiClient.SaveEntityDefine(schema)).AssertFail();
    }


    [Fact]
    public async Task SaveSchema()
    {
        var schema = TestSchema();
        await _accountApiClient.EnsureLogin();
        (await _schemaApiClient.SaveEntityDefine(schema)).AssertSuccess();
        (await _schemaApiClient.GetLoadedEntity(schema.Name)).AssertSuccess();
    }

    [Fact]
    public async Task SaveSchemaTwice()
    {
        var schema = TestSchema();
        await _accountApiClient.EnsureLogin();
        var res = (await _schemaApiClient.SaveEntityDefine(schema)).AssertSuccess();
        (await _schemaApiClient. SaveEntityDefine(res)).AssertSuccess();
    }
    
    [Fact]
    public async Task SaveSchema_Update()
    {
        var schema = TestSchema();
        await _accountApiClient.EnsureLogin();
        schema = (await _schemaApiClient.SaveEntityDefine(schema)).AssertSuccess();
        schema.Settings.Entity = schema.Settings.Entity! with { DefaultPageSize = 10 };
        (await _schemaApiClient.SaveEntityDefine(schema)).AssertSuccess();
        var entity = (await _schemaApiClient.GetLoadedEntity(schema.Name)).AssertSuccess();
        Assert.Equal(10,entity.DefaultPageSize);
    }

    [Fact]
    public async Task Delete_Success()
    {
        var schema = TestSchema();
        await _accountApiClient.EnsureLogin();
        schema = (await _schemaApiClient.SaveEntityDefine(schema)).AssertSuccess();
        (await _schemaApiClient.DeleteSchema(schema.Id)).AssertSuccess();
        (await _schemaApiClient.GetLoadedEntity(schema.Name)).AssertFail();
   }

    [Fact]
    public async Task GetAll_NUllType()
    {
        await _accountApiClient.EnsureLogin();
        var items = (await _schemaApiClient.GetAll("")).AssertSuccess();
        var len = items.Length;
        var schema = TestSchema();
        await _schemaApiClient.SaveEntityDefine(schema);
        items = (await _schemaApiClient.GetAll("")).AssertSuccess();
        Assert.Equal(len + 1, items.Length );
    }

    [Fact]
    public async Task GetAll_EntityType()
    {
        await _accountApiClient.EnsureLogin();
        var items = (await _schemaApiClient.GetAll(SchemaType.Menu)).AssertSuccess();
        Assert.Single(items);
    }


    private static Entity RandomTestEntity()
    {
        var name = Guid.NewGuid().ToString("N");
        return new Entity(
            Name: $"IntegrationTest{name}",
            PrimaryKey: "id",
            TableName: $"integration_test{name}",
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
    }

    private static Schema TestSchema()
    {
        var entity = RandomTestEntity();
        return new Schema
        {
            Name = entity.Name,
            Type = SchemaType.Entity,
            Settings = new Settings
            {
                Entity = entity
            }
        };
    }
}