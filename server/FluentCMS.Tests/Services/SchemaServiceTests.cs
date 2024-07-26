using FluentCMS.Models;
using FluentCMS.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SQLitePCL;
using Utils.DataDefinitionExecutor;
using Utils.KateQueryExecutor;
using Utils.QueryBuilder;
using Attribute = Utils.QueryBuilder.Attribute;

namespace FluentCMS.Tests.Services;

public class SchemaServiceTests :IAsyncLifetime
{
    private readonly SchemaService _schemaService;
    private readonly string _dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.db");
    public SchemaServiceTests()
    {
        Batteries.Init();
        var connectionString = $"Data Source={_dbFilePath}";
        var defineLogger = new Mock<ILogger<SqliteDefinitionExecutor>>();
        var definitionExecutor = new SqliteDefinitionExecutor(connectionString,defineLogger.Object);
        var providerLogger = new Mock<ILogger<SqliteKateProvider>>();
        var sqlite = new SqliteKateProvider(connectionString,providerLogger.Object);
        var kateQueryExecutor = new KateQueryExecutor(sqlite);
        _schemaService = new SchemaService(definitionExecutor, kateQueryExecutor);
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
        await _schemaService.AddSchemaTable();
        await _schemaService.AddSchemaTable();
    }
    [Fact]
    public async Task AddTopMenuBar_Success()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();
        var menus = await _schemaService.GetByIdOrName(SchemaName.TopMenuBar,false);
        Assert.NotNull(menus);
    }
  
    [Fact]
    public async Task SaveEntity_Insert()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();
        await _schemaService.Save(TestSchema());
        var entity = _schemaService.GetEntityByName(TestEntity().Name);
        Assert.NotNull(entity);
        Assert.True(entity.Id > 0);
    }

    [Fact]
    public async Task SaveEntity_Update()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();
        var schema = await _schemaService.Save(TestSchema());
        schema.Settings.Entity!.TableName = "test2";
        await _schemaService.Save(schema);
        var entity = await _schemaService.GetEntityByName(TestEntity().Name);
        Assert.Equal("test2",entity!.TableName);
    }

    [Fact]
    public async Task Delete_Success()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();

        var schema = await _schemaService.GetByIdOrNameDefault(TestEntity().Name) ??
                     await _schemaService.Save(TestSchema());
           
        await _schemaService.Delete(schema.Id);
        schema = await _schemaService.GetByIdOrNameDefault(TestEntity().Name);
        Assert.Null(schema);
    }

    [Fact]
    public async Task GetAll_NUllType()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();
        await _schemaService.Save(TestSchema());
        var items = await _schemaService.GetAll("");
        Assert.Equal(2, items.Length );
    }

    [Fact]
    public async Task GetAll_EntityType()
    {
        await _schemaService.AddSchemaTable();
        await _schemaService.AddTopMenuBar();
        await _schemaService.Save(TestSchema());
        var items = await _schemaService.GetAll(SchemaType.Menu);
        Assert.Single(items);
    }

    private static Entity TestEntity() => new Entity
    {
        Name = "Test",
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