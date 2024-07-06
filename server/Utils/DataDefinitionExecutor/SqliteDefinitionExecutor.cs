using Microsoft.Data.Sqlite;

namespace Utils.DataDefinitionExecutor;

public sealed class SqliteDefinitionExecutor(string connectionString, bool debug) : IDefinitionExecutor
{
   public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions)
   {
      var sql = GenerateCreateTableSql(tableName, columnDefinitions);
      await ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync());
   }
   
   public async Task AddColumns(string tableName, ColumnDefinition[] columnDefinitions)
   {
      if (columnDefinitions.Length == 0)
      {
         return;
      }
      var sqlStrs = GenerateAddColumnSql(tableName, columnDefinitions);
      await using var connection = new SqliteConnection(connectionString);
      await connection.OpenAsync();
      await using var tran = connection.BeginTransaction();
      foreach (var sql in sqlStrs)
      {
         await using var command = new SqliteCommand(sql, connection, tran);
         command.ExecuteNonQuery();
      }
      await tran.CommitAsync();
   }
   public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName)
   {
      var sql = $"PRAGMA table_info({tableName})";
      /*cid name tuype notnull dflt_value, pk */
      return await ExecuteQuery(sql, async command =>
      {
         await using var reader = await command.ExecuteReaderAsync();
         var columnDefinitions = new List<ColumnDefinition>();
         while (await reader.ReadAsync())
         {
            columnDefinitions.Add(CreateColumnDefinition(reader.GetString(1), reader.GetString(2)));
         }

         return columnDefinitions.ToArray();
      }, ("tableName", tableName));
   }
   
    private async Task<T> ExecuteQuery<T>(string sql, Func<SqliteCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(sql, connection);

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        return await executeFunc(command);
    }

    private string[] GenerateAddColumnSql(string tableName, ColumnDefinition[] columnDefinitions)
    {
        return columnDefinitions.
            Select(x => $"Alter Table {tableName} ADD COLUMN {x.ColumnName} {GetSqlType(x)} ")
            .ToArray();
    }

    private string GenerateCreateTableSql(string tableName, ColumnDefinition[] columns)
    {
        var columnDefinitionStrs = columns.Select(column => column.ColumnName.ToLower() switch
        {
            "id" => "id INTEGER  primary key autoincrement",
            "deleted" => "deleted INTEGER   default 0",
            "created_at" => "created_at integer default (datetime('now','localtime'))",
            "updated_at" => "updated_at integer default (datetime('now','localtime'))",
            _ => $"{column.ColumnName} {GetSqlType(column)}"
        });
        var ret= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
        ret += $@"CREATE TRIGGER update_{tableName}_updated_at BEFORE UPDATE ON {tableName} FOR EACH ROW
            BEGIN UPDATE {tableName} SET updated_at = (datetime('now','localtime')) WHERE id = OLD.id; 
            END;";
        return ret;
    }

    private string GetSqlType(ColumnDefinition column)
    {
        return column.DataType switch
        {
            DataType.Int => "INTEGER",
            DataType.Text => "TEXT",
            DataType.Datetime => "INTEGER",
            DataType.String => "TEXT",
            _ => throw new NotSupportedException($"Type {column.DataType} is not supported")
        };
    }

    private ColumnDefinition CreateColumnDefinition(string columnName, string dataType)
    {
        dataType = dataType.ToLower();
        return new ColumnDefinition
        {
            ColumnName = columnName,
            DataType = dataType switch
            {
                "integer" => DataType.Int,
                "text" => DataType.Text,
                "datetime" => DataType.Text,
                _ when dataType.StartsWith("varchar") => DataType.String,
                _ => DataType.Text
            },
        };
    }
}