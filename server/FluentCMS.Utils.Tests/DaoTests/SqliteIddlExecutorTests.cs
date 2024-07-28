using Microsoft.Extensions.Logging;
using Moq;
using SQLitePCL;
using FluentCMS.Utils.DataDefinitionExecutor;
namespace Utils.Tests.DaoTests;
public class SqliteDefinitionExecutorTests: IAsyncLifetime
{
    private readonly SqliteDefinitionExecutor _executor;
    private readonly string _dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.db");

    public SqliteDefinitionExecutorTests()
    {
        var logger = new Mock<ILogger<SqliteDefinitionExecutor>>();
        var connectionString = $"Data Source={_dbFilePath}";
        _executor = new SqliteDefinitionExecutor(connectionString,logger.Object);
        Batteries.Init();
    }
    [Fact]
    public async Task GetDefinitionOfNotExistsTable()
    {
        var ret = await _executor.GetColumnDefinitions($@"table{DateTime.Now.TimeOfDay.Milliseconds}");
        Assert.True(ret != null && ret.Length == 0);
    }
    [Fact]
    public async Task AddColumns()
    {
        await _executor.CreateTable("posts1", [
            new ColumnDefinition { ColumnName = "id"},
            new ColumnDefinition { ColumnName = "created_at"},
            new ColumnDefinition { ColumnName = "updated_at"},
            new ColumnDefinition { ColumnName = "title", DataType = DataType.String}
        ]);
            
        await _executor.AlterTableAddColumns("posts1", new[]
        {
            new ColumnDefinition { ColumnName = "excerpt", DataType = DataType.Text},
            new ColumnDefinition { ColumnName = "release_date", DataType = DataType.Datetime},
        });
    }
    [Fact]
    public async Task CreateTable()
    {
        await _executor.CreateTable("posts1", new[]
        {
            new ColumnDefinition { ColumnName = "id"},
            new ColumnDefinition { ColumnName = "created_at"},
            new ColumnDefinition { ColumnName = "updated_at"},
            new ColumnDefinition { ColumnName = "title", DataType = DataType.String},
            new ColumnDefinition { ColumnName = "body", DataType = DataType.Text},
            new ColumnDefinition { ColumnName = "published_at", DataType = DataType.Datetime},
            new ColumnDefinition { ColumnName = "costs", DataType = DataType.Int},
            
        });
    }
    // Called before each test
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    // Called after each test
    public Task DisposeAsync()
    {
        if (File.Exists(_dbFilePath))
        {
            File.Delete(_dbFilePath);        
        }
        return Task.CompletedTask;
    }
}