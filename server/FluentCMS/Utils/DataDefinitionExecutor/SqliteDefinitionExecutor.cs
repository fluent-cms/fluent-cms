using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Utils.DataDefinitionExecutor;

public sealed class SqliteDefinitionExecutor(string connectionString, ILogger<SqliteDefinitionExecutor> logger) : IDefinitionExecutor
{
    public bool TryParseDataType(string s, string type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            DataType.Datetime or DataType.String or DataType.Text or DataType.Na => new DatabaseTypeValue(s),
            DataType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            _ => null
        };
        return result != null;
    }

   public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
   {
       var columnDefinitionStrs = columnDefinitions.Select(column => column.ColumnName.ToLower() switch
       {
           "id" => "id INTEGER  primary key autoincrement",
           "deleted" => "deleted INTEGER   default 0",
           "created_at" => "created_at integer default (datetime('now','localtime'))",
           "updated_at" => "updated_at integer default (datetime('now','localtime'))",
           _ => $"{column.ColumnName} {DataTypeToString(column.DataType)}"
       });
        
       var sql= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
       sql += $@"
            CREATE TRIGGER update_{tableName}_updated_at 
                BEFORE UPDATE ON {tableName} 
                FOR EACH ROW
            BEGIN 
                UPDATE {tableName} SET updated_at = (datetime('now','localtime')) WHERE id = OLD.id; 
            END;";
      await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
   }

   public async Task AlterTableAddColumns(string tableName, ColumnDefinition[] columnDefinitions, CancellationToken cancellationToken)
   {
       var parts = columnDefinitions.Select(x =>
           $"Alter Table {tableName} ADD COLUMN {x.ColumnName} {DataTypeToString(x.DataType)}"
       );
       var sql = string.Join(";", parts.ToArray());
       await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync(cancellationToken));
   }

   public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName,CancellationToken cancellationToken)
   {
      var sql = $"PRAGMA table_info({tableName})";
      return await ExecuteQuery(sql, async command =>
      {
         await using var reader = await command.ExecuteReaderAsync();
         var columnDefinitions = new List<ColumnDefinition>();
         while (await reader.ReadAsync(cancellationToken))
         {
            /*cid, name, type, notnull, dflt_value, pk */
            columnDefinitions.Add(new ColumnDefinition
           ( 
                ColumnName : reader.GetString(1),
                DataType : StringToDataType(reader.GetString(2))
            ));
         }
         return columnDefinitions.ToArray();
      });
   }
   
    private string DataTypeToString(string dataType)
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

    private string StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => DataType.Int,
            _ => DataType.Text
        };
    }

    private async Task<T> ExecuteQuery<T>(string sql, Func<SqliteCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(sql, connection);

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }
        return await executeFunc(command);
    }
}