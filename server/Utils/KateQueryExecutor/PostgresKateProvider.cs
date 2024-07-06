using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;

public class PostgresKateProvider(string connectionString, bool isDebug) : IKateProvider
{
    private readonly Compiler _compiler = new PostgresCompiler();

    public async Task<T> Execute<T>(Query? query, Func<QueryFactory, Task<T>> queryFunc)
    {
        if (isDebug && query is not null)
        {
            Console.WriteLine(query);
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return await queryFunc(db);
    }
}