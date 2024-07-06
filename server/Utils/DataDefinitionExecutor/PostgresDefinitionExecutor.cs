using System.Data;
using Npgsql;

namespace Utils.DataDefinitionExecutor;

public class PostgresDefinitionExecutor(string connectionString, bool debug):IDefinitionExecutor
{
    public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions)
    {
        var columnDefinitionStrs = columnDefinitions.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "id SERIAL PRIMARY KEY",
            "deleted" => "BOOLEAN DEFAULT FALSE",
            "created_at" => "created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            "updated_at" => "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            _ => $"{column.ColumnName} {DataTypeToString(column.DataType)}"
        });
        
        var sql= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
        sql += $@"
            CREATE TRIGGER update_{tableName}_updated_at 
                BEFORE UPDATE ON {tableName} 
                FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column();";
     
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync());
    }
   
    public async Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions)
    {
        var parts = columnDefinitions.Select(x =>
            $"Alter Table {tableName} ADD COLUMN {x.ColumnName} {DataTypeToString(x.DataType)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync());
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
                    DataType = StringToDataType(reader.GetString(1))
                });
            }
            return columnDefinitions.ToArray();
        }, ("tableName", tableName));
    }
    
    
    private string DataTypeToString(DataType dataType)
    {
        return dataType switch
        {
            DataType.Int => "INTEGER",
            DataType.Text => "TEXT",
            DataType.Datetime => "INTEGER",
            DataType.String => "TEXT",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    private DataType StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => DataType.Int,
            "text" => DataType.Text,
            "datetime" => DataType.Text,
            _ when s.StartsWith("varchar") => DataType.String,
            _ => DataType.Text
        };
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