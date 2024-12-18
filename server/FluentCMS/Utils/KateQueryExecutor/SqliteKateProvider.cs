using Microsoft.Data.Sqlite;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.KateQueryExecutor;


public class SqliteKateProvider(KateProviderOption option, ILogger<SqliteKateProvider> logger) : IKateProvider
{
    private readonly Compiler _compiler = new SqliteCompiler();

    public async Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        var connection = new SqliteConnection(option.ConnectionString);
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }
}