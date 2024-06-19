using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.Dao;

public class PgDao(string connectionString):IDao
{
    Compiler _compiler = new PostgresCompiler();
    public async Task<dynamic[]> Get(Query query)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return (await db.GetAsync(query)).ToArray();
    }

    public async Task<int> Count(Query query)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var db = new QueryFactory(connection, _compiler);
        return await db.CountAsync<int>(query);
    }
    
    public async Task<string> GetPrimaryKeyColumn(string tableName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var sql = @"SELECT pg_attribute.attname
                                    FROM pg_index, pg_class, pg_attribute
                                    WHERE pg_class.relname = @tableName
                                      AND pg_class.oid = pg_index.indrelid
                                      AND pg_index.indkey[0] = pg_attribute.attnum
                                      AND pg_attribute.attrelid = pg_class.oid
                                      AND pg_index.indisprimary;";
        await using var command = new NpgsqlCommand(sql , connection);

        command.Parameters.AddWithValue("tableName", tableName);

        var primaryKeyColumn = await command.ExecuteScalarAsync();

        return primaryKeyColumn?.ToString()??"";
    }

    public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName)
    {
        var columnDefinitions = new List<ColumnDefinition>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                  FROM information_schema.columns
                  WHERE table_name = @tableName;", connection);

        command.Parameters.AddWithValue("tableName", tableName);

        await using var reader = command.ExecuteReader();
        while (await reader.ReadAsync())
        {
            var column = new ColumnDefinition
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? "N/A" : reader.GetValue(2),
                IsNullable = reader.GetString(3),
                DefaultValue = reader.IsDBNull(4) ? "N/A" : reader.GetValue(4)
            };

            columnDefinitions.Add(column);
        }
        return columnDefinitions.ToArray();
    }
}