using FluentCMS.Cms.Services;
using FluentCMS.Cms.Models;
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
    private readonly EntitySchemaService _entitySchemaService;
    private readonly QuerySchemaService _querySchemaService;
    
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
        _entitySchemaService = new EntitySchemaService(_schemaService, definitionExecutor);
        _querySchemaService = new QuerySchemaService(_schemaService,_entitySchemaService);
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
        var menus = await _schemaService.GetByNameDefault(SchemaName.TopMenuBar,SchemaType.Menu);
        Assert.NotNull(menus);
    }
  
    [Fact]
    public async Task SaveSchema_Insert()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        await _schemaService.Save(TestSchema());
        var entity = _entitySchemaService.GetByNameDefault(TestEntity().Name, false,default);
        Assert.NotNull(entity);
        Assert.True(entity.Id > 0);
    }
    [Fact]
    public async Task SaveSchemaDefine_Twice()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var schema = await _entitySchemaService.SaveTableDefine(TestSchema());
        await _entitySchemaService.SaveTableDefine(schema);
    }

    [Fact]
    public async Task SaveSchema_Update()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();
        var schema = await _schemaService.Save(TestSchema());
        schema.Settings.Entity!.TableName = "test2";
        await _schemaService.Save(schema);
        var entity = await _entitySchemaService.GetByNameDefault(TestEntity().Name, false,default);
        Assert.False(entity.IsFailed);
        Assert.Equal("test2",entity.Value.TableName);
    }

    [Fact]
    public async Task Delete_Success()
    {
        await _schemaService.EnsureSchemaTable();
        await _schemaService.EnsureTopMenuBar();

        var schema = await _schemaService.GetByNameDefault(TestEntity().Name, SchemaType.Entity) ??
                     await _schemaService.Save(TestSchema());
           
        await _schemaService.Delete(schema.Id);
        schema = await _schemaService.GetByNameDefault(TestEntity().Name,SchemaType.Entity);
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