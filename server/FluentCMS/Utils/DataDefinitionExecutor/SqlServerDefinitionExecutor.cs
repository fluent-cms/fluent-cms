using System.Data.SqlClient;

namespace FluentCMS.Utils.DataDefinitionExecutor;

public class SqlServerDefinitionExecutor(string connectionString, ILogger<SqlServerDefinitionExecutor> logger ) : IDefinitionExecutor
{
    public bool TryParseDataType(string s, string type, out object value)
    {
        value = s;
        var ret = true;
        switch (type)
        {
            case DataType.Int:
                ret = int.TryParse(s, out var resultInt);
                value = resultInt;
                break;
        }

        return ret;
    }

    public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
    {
        var columnDefinitionStrs = columnDefinitions.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "[id] INT IDENTITY(1,1) PRIMARY KEY",
            "deleted" => "[deleted] BIT DEFAULT 0",
            "created_at" => "[created_at] DATETIME DEFAULT GETDATE()",
            "updated_at" => "[updated_at] DATETIME DEFAULT GETDATE()",
            _ => $"[{column.ColumnName}] {DataTypeToString(column.DataType)}"
        });

        var sql = $"CREATE TABLE [{tableName}] ({string.Join(", ", columnDefinitionStrs)});";
        
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
        sql = $"""
               CREATE TRIGGER trg_{tableName}_updated_at 
               ON [{tableName}] 
               AFTER UPDATE
               AS 
               BEGIN
                   SET NOCOUNT ON;
                   UPDATE [{tableName}]
                   SET [updated_at] = GETDATE()
                   FROM inserted i
                   WHERE [{tableName}].[id] = i.[id];
               END;
               """;

        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
    }

    public async Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
    {
        if (columnDefinitions.Length == 0)
        {
            return;
        }
        
        var parts = columnDefinitions.Select(x =>
            $"ALTER TABLE [{tableName}] ADD [{x.ColumnName}] {DataTypeToString(x.DataType)}"
        );
        var sql = string.Join(";", parts.ToArray());
        await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
    }

    public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName, CancellationToken cancellationToken)
    {
        var sql = @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName";

        return await ExecuteQuery(sql, async command =>
        {
            var columnDefinitions = new List<ColumnDefinition>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync())
            {
                columnDefinitions.Add(new ColumnDefinition
                ( 
                    ColumnName : reader.GetString(0),
                    DataType : StringToDataType(reader.GetString(1))
                ));
            }

            return columnDefinitions.ToArray();
        }, ("tableName", tableName));
    }

    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            DataType.Int => "INT",
            DataType.Text => "TEXT",
            DataType.Datetime => "DATETIME",
            DataType.String => "VARCHAR(255)",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    private string StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "int" => DataType.Int,
            "text" => DataType.Text,
            "datetime" => DataType.Datetime,
            _ => DataType.String
        };
    }

    // Use callback instead of return QueryFactory to ensure proper disposing connection
    private async Task<T> ExecuteQuery<T>(string sql, Func<SqlCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }
}

