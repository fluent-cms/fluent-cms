using SQLitePCL;
using Utils.DataDefinitionExecutor;

namespace Utils.Tests.DaoTests;

public class SqliteIddlExecutorTests
{
    [Fact]
    public async Task GetDefinitionOfNotExistsTable()
    {

        Batteries.Init();
        var dao = new SqliteDefinitionExecutor("Data Source=test.db", true);
        var ret = await dao.GetColumnDefinitions($@"table{DateTime.Now.TimeOfDay.Milliseconds}");
        Assert.True(ret != null && ret.Length == 0);
    }
    [Fact]
    public async Task AddColumns()
    {
        Batteries.Init();
        var dao = new SqliteDefinitionExecutor("Data Source=test.db", true);
        await dao.AddColumns("posts1", new[]
        {
            new ColumnDefinition { ColumnName = "excerpt", DataType = DataType.Text},
            new ColumnDefinition { ColumnName = "release_date", DataType = DataType.Datetime},
        });
    }
    [Fact]
    public async Task CreateTable()
    {
        Batteries.Init();
        var dao = new SqliteDefinitionExecutor("Data Source=test.db", true);
        await dao.CreateTable("posts1", new[]
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
}