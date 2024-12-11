using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.KateQueryExecutor;
using Microsoft.Extensions.Logging;

public class PostgresKateProvider(NpgsqlDataSource dataSource, ILogger<PostgresKateProvider> logger) : IKateProvider
{
    private readonly Compiler _compiler = new PostgresCompiler();

    public async Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }
}