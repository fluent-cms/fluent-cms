using Npgsql;

namespace FluentCMS.Utils;

public class PgDao(string connectionString):IDao
{
    public async Task<string> GetPrimaryKeyColumn(string tableName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var command = new NpgsqlCommand(
            @"SELECT pg_attribute.attname
                  FROM pg_index, pg_class, pg_attribute
                  WHERE pg_class.relname = @tableName
                    AND pg_class.oid = pg_index.indrelid
                    AND pg_index.indkey[0] = pg_attribute.attnum
                    AND pg_attribute.attrelid = pg_class.oid
                    AND pg_index.indisprimary;", connection);

        command.Parameters.AddWithValue("tableName", tableName);

        var primaryKeyColumn = await command.ExecuteScalarAsync();

        return primaryKeyColumn?.ToString()??"";
    }

    public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName)
    {
        var columnDefinitions = new List<ColumnDefinition>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var command = new NpgsqlCommand(
            @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                  FROM information_schema.columns
                  WHERE table_name = @tableName;", connection);

        command.Parameters.AddWithValue("tableName", tableName);

        await using var reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                ColumnDefinition column = new ColumnDefinition
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