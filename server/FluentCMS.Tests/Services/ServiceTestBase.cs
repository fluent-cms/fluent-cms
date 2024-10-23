using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using Microsoft.Extensions.Logging;
using Moq;
using SQLitePCL;

namespace FluentCMS.Tests.Services;

public abstract class ServiceTestBase:IAsyncLifetime
{
    private readonly string _dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.db");
    protected readonly IServiceProvider ServiceProvider = new Mock<IServiceProvider>().Object;
    protected readonly HookRegistry HookRegistry = new();
    protected readonly KateQueryExecutor KateQueryExecutor;
    protected readonly SqliteDefinitionExecutor SqliteDefinitionExecutor;

    protected ServiceTestBase()
    {
        var connectionString = $"Data Source={_dbFilePath}";
        var defineLogger = new Mock<ILogger<SqliteDefinitionExecutor>>();
        SqliteDefinitionExecutor= new SqliteDefinitionExecutor(connectionString,defineLogger.Object);
        
        var kateProviderLogger = new Mock<ILogger<SqliteKateProvider>>();
        var sqlite = new SqliteKateProvider(connectionString,kateProviderLogger.Object);
        KateQueryExecutor = new KateQueryExecutor(sqlite, 30);
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
    
    // Called before each test
    public Task InitializeAsync()
    {
        Batteries.Init();
        return Task.CompletedTask;
    }

    // Called after each test
    public Task DisposeAsync()
    {
        CleanDb();
        return Task.CompletedTask;
    }
    
 
}