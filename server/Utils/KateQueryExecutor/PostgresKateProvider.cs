using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;
using Microsoft.Extensions.Logging;

public class PostgresKateProvider(string connectionString, ILogger<PostgresKateProvider> logger) : IKateProvider
{
    private readonly Compiler _compiler = new PostgresCompiler();

    public async Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }
}