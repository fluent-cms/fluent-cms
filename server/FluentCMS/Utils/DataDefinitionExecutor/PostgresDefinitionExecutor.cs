using Npgsql;

namespace FluentCMS.Utils.DataDefinitionExecutor;

public class PostgresDefinitionExecutor(string connectionString, ILogger<PostgresDefinitionExecutor> logger):IDefinitionExecutor
{
    public bool CastToDatabaseDataType(string s, string type, out object? result)
    {
        result = s;
        var ret = true;
        switch (type)
        {
            case DataType.Int:
                ret = int.TryParse(s, out var resultInt);
                result = resultInt;
                break;
            case DataType.Datetime:
                ret = int.TryParse(s, out var resultDateTime);
                result = resultDateTime;
                break;
        }
        return ret;
    }

    public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
    {
        var columnDefinitionStrs = columnDefinitions.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "id SERIAL PRIMARY KEY",
            "deleted" => "deleted BOOLEAN DEFAULT FALSE",
            "created_at" => "created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            "updated_at" => "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
            _ => $"\"{column.ColumnName}\" {DataTypeToString(column.DataType)}"
        });
        
        var sql= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
        sql += $"""
                CREATE OR REPLACE FUNCTION __update_updated_at_column()
                    RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                
                CREATE TRIGGER update_{tableName}_updated_at 
                                BEFORE UPDATE ON {tableName} 
                                FOR EACH ROW
                EXECUTE FUNCTION __update_updated_at_column();
                """;
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
    }
   
    public async Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
    {
        var parts = columnDefinitions.Select(x =>
            $"Alter Table {tableName} ADD COLUMN \"{x.ColumnName}\" {DataTypeToString(x.DataType)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
    }
    
    public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken)
    {
        var sql = @"SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
                FROM information_schema.columns
                WHERE table_name = @tableName;";

        return await ExecuteQuery(sql, async command =>
        {
            await using var reader = command.ExecuteReader();
            var columnDefinitions = new List<ColumnDefinition>();
            while (await reader.ReadAsync(cancellationToken))
            {
                columnDefinitions.Add(new ColumnDefinition(reader.GetString(0),reader.GetString(1)));
            }
            return columnDefinitions.ToArray();
        }, ("tableName", tableName));
    }
    
    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            DataType.Int => "INTEGER",
            DataType.Text => "TEXT",
            DataType.Datetime => "TIMESTAMP",
            DataType.String => "varchar(255)",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    private string StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => DataType.Int,
            "text" => DataType.Text,
            "timestamp" => DataType.Datetime,
            _ => DataType.String
        };
    }
    
    //use callback  instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, Func<NpgsqlCommand, Task<T>> executeFunc, params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
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