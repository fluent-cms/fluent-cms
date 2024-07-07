using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;

public class SqliteKateProvider(string connectionString , ILogger<SqliteKateProvider> logger):IKateProvider
{
    private readonly Compiler _compiler = new SqliteCompiler();

    public async Task<T> Execute<T>(Query? query , Func<QueryFactory, Task<T>> queryFunc)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }
}