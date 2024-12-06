using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.KateQueryExecutor;

public class SqlServerKateProvider(string connectionString, ILogger<SqlServerKateProvider> logger) : IKateProvider
{
    private readonly Compiler _compiler = new SqlServerCompiler();

    public async Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        var connection = new SqlConnection(connectionString);
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }
}