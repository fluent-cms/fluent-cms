using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;
using Query = SqlKata.Query;

namespace FluentCMS.Utils.Dao;

public class PgDao(string connectionString, bool debug):IDao
{
    Compiler _compiler = new PostgresCompiler();

    public async Task<int?> Exec(Query? query)
    {
        Log(query);
        return query is null ? null:await ExecuteKateQuery(async db => await db.ExecuteAsync(query));
    }

    public async Task<object?> GetOne(Query? query)
    {
        Log(query);
        return query is null ? null : await ExecuteKateQuery(async db => await db.FirstOrDefaultAsync(query));
    }
    public async Task<dynamic[]> Get(Query query)
    {
        Log(query);
        return await ExecuteKateQuery(async db => (await db.GetAsync(query)).ToArray());
    }

    public async Task<int> Count(Query query)
    {
        Log(query);
        return await ExecuteKateQuery(async db => await db.CountAsync<int>(query));
    }

    public async Task<string> GetPrimaryKeyColumn(string tableName)
    {
        var sql = @"SELECT pg_attribute.attname
                        FROM pg_index, pg_class, pg_attribute
                        WHERE pg_class.relname = @tableName
                          AND pg_class.oid = pg_index.indrelid
                          AND pg_index.indkey[0] = pg_attribute.attnum
                          AND pg_attribute.attrelid = pg_class.oid
                          AND pg_index.indisprimary;";

        return await ExecuteQuery(sql, async command =>
        {
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "";
        }, ("tableName", tableName));
    }

    public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName)
    {
        var sql = @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_name = @tableName;";

        return await ExecuteQuery(sql, async command =>
        {
            await using var reader = command.ExecuteReader();
            var columnDefinitions = new List<ColumnDefinition>();
            while (await reader.ReadAsync())
            {
                var column = new ColumnDefinition
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? "N/A" : reader.GetValue(2).ToString(),
                    IsNullable = reader.GetString(3),
                    DefaultValue = reader.IsDBNull(4) ? "N/A" : reader.GetValue(4).ToString()
                };

                columnDefinitions.Add(column);
            }
            return columnDefinitions.ToArray();
        }, ("tableName", tableName));
    }
    
    //use callback  instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, Func<NpgsqlCommand, Task<T>> executeFunc, params (string, object)[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
    private async Task<T> ExecuteKateQuery<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return await queryFunc(db);
    }

    private void Log(Query? query)
    {
        if (!debug || query is null)
        {
            return;
        }
        Console.WriteLine(_compiler.Compile(query));
    }
}