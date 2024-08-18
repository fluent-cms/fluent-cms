using FluentCMS.Cms.Services;
using FluentCMS.Models;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.Logging;
using Moq;
using SQLitePCL;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Tests.Services;

public class SchemaServiceTests :IAsyncLifetime
{
    private readonly SchemaService _schemaService;
    private readonly string _dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.db");
    public SchemaServiceTests()
    {
        var connectionString = $"Data Source={_dbFilePath}";
        var defineLogger = new Mock<ILogger<SqliteDefinitionExecutor>>();
        var definitionExecutor = new SqliteDefinitionExecutor(connectionString,defineLogger.Object);
        var kateProviderLogger = new Mock<ILogger<SqliteKateProvider>>();
        var sqlite = new SqliteKateProvider(connectionString,kateProviderLogger.Object);
        var kateQueryExecutor = new KateQueryExecutor(sqlite, 30);
        var provider = new Mock<IServiceProvider>();

        HookRegistry registry = new();
        _schemaService = new SchemaService(definitionExecutor, kateQueryExecutor,registry,provider.Object);
        Batteries.Init();
    }
    private void CleanDb()
    {
        if (!File.Exists(_dbFilePath))
        {
            return;
        }

        Console.WriteLine($"deleting ${_dbFilePath}");
        File.Delete(_dbFilePath);
    }

    [Fact]
    public async Task AddSchemaTable_NoException()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureSchemaTable();
    }
    [Fact]
    public async Task AddTopMenuBar_Success()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var menus = await _schemaService.GetByNameVerify(SchemaName.TopMenuBar,false);
        Assert.NotNull(menus);
    }
  
    [Fact]
    public async Task SaveSchema_Insert()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        await _schemaService.Save(TestSchema());
        var entity = _schemaService.GetEntityByNameOrDefault(TestEntity().Name);
        Assert.NotNull(entity);
        Assert.True(entity.Id > 0);
    }
    [Fact]
    public async Task SaveSchemaDefine_Twice()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var schema = await _schemaService.SaveTableDefine(TestSchema());
        await _schemaService.SaveTableDefine(schema);
    }

    [Fact]
    public async Task SaveSchema_Update()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var schema = await _schemaService.Save(TestSchema());
        schema.Settings.Entity!.TableName = "test2";
        await _schemaService.Save(schema);
        var entity = await _schemaService.GetEntityByNameOrDefault(TestEntity().Name);
        Assert.False(entity.IsFailed);
        Assert.Equal("test2",entity.Value.TableName);
    }

    [Fact]
    public async Task Delete_Success()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();

        var schema = await _schemaService.GetByNameDefault(TestEntity().Name) ??
                     await _schemaService.Save(TestSchema());
           
        await _schemaService.Delete(schema.Id);
        schema = await _schemaService.GetByNameDefault(TestEntity().Name);
        Assert.Null(schema);
    }

    [Fact]
    public async Task GetAll_NUllType()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        await _schemaService.Save(TestSchema());
        var items = await _schemaService.GetAll("");
        Assert.Equal(2, items.Length );
    }

    [Fact]
    public async Task GetAll_EntityType()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        await _schemaService.Save(TestSchema());
        var items = await _schemaService.GetAll(SchemaType.Menu);
        Assert.Single(items);
    }

    private static Entity TestEntity() => new Entity
    {
        Name = "Test",
        TableName = "Test",
        Attributes =
        [
            new Attribute
            {
                Field = "Title",
                DataType = DataType.String
            }
        ]
    };

    private static Schema TestSchema() => new Schema
    {
        Name = "test",
        Type = SchemaType.Entity,
        Settings = new Settings
        {
            Entity = TestEntity()
        }
    };
    
    // Called before each test
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    // Called after each test
    public Task DisposeAsync()
    {
        CleanDb();
        return Task.CompletedTask;
    }
}