using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;

public class SqliteKateProvider(string connectionString , bool isDebug):IKateProvider
{
    private readonly Compiler _compiler = new SqliteCompiler();

    public async Task<T> Execute<T>(Query? query , Func<QueryFactory, Task<T>> queryFunc)
    {
        if (isDebug && query is not null)
        {
            Console.WriteLine(_compiler.Compile(query));
        }
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return await queryFunc(db);
    }
}