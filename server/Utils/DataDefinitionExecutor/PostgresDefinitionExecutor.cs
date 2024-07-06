using Npgsql;

namespace Utils.DataDefinitionExecutor;

public class PostgresDefinitionExecutor(string connectionString, bool debug):IDefinitionExecutor
{
    public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions)
    {
        throw new Exception("todo");
    }
   
    public async Task AddColumns(string tableName, ColumnDefinition[] columnDefinitions)
    {
        throw new Exception("no implementaion");
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
                columnDefinitions.Add(new ColumnDefinition
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1) switch
                    {
                        "integer" => DataType.Int,
                        "text" => DataType.String,
                        "datetime" => DataType.Datetime,
                        _ => DataType.String,
                    }
                });
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
}