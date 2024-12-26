using System.Data;
using Microsoft.Data.Sqlite;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.RelationDbDao;

public sealed class SqliteDao(SqliteConnection connection, ILogger<SqliteDao> logger) : IDao
{
    private readonly Compiler _compiler = new SqliteCompiler();

    public async Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc)
    {
        var db = new QueryFactory(connection, _compiler);
        db.Logger = result => logger.LogInformation(result.ToString());
        return await queryFunc(db);
    }

    public async ValueTask<IDbTransaction> BeginTransaction() => await connection.BeginTransactionAsync();

    public bool TryParseDataType(string s, string type, out DatabaseTypeValue? result)
    {
        result = type switch
        {
            ColumnType.Datetime or ColumnType.String or ColumnType.Text  => new DatabaseTypeValue(s),
            ColumnType.Int when int.TryParse(s, out var resultInt) => new DatabaseTypeValue(I: resultInt),
            _ => null
        };
        return result != null;
    }

    public async ValueTask<IDbTransaction> BeginTransactionAsync(CancellationToken ct = default)=> await connection.BeginTransactionAsync(ct);
    

    public async Task CreateTable(string tableName, IEnumerable<Column> cols, CancellationToken ct,IDbTransaction? tx)
   {
       var columnDefinitionStrs = cols.Select(column => column.Name.ToLower() switch
       {
           "id" => "id INTEGER  primary key autoincrement",
           "deleted" => "deleted INTEGER   default 0",
           "created_at" => "created_at integer default (datetime('now','localtime'))",
           "updated_at" => "updated_at integer default (datetime('now','localtime'))",
           _ => $"{column.Name} {DataTypeToString(column.Type)}"
       });
        
       var sql= $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitionStrs)});";
       sql += $@"
            CREATE TRIGGER update_{tableName}_updated_at 
                BEFORE UPDATE ON {tableName} 
                FOR EACH ROW
            BEGIN 
                UPDATE {tableName} SET updated_at = (datetime('now','localtime')) WHERE id = OLD.id; 
            END;";
      await ExecuteQuery(sql,tx, async cmd => await cmd.ExecuteNonQueryAsync(ct));
   }

   public async Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct,IDbTransaction? tx)
   {
       var parts = cols.Select(x =>
           $"Alter Table {table} ADD COLUMN {x.Name} {DataTypeToString(x.Type)}"
       );
       var sql = string.Join(";", parts.ToArray());
       await ExecuteQuery(sql, tx,async cmd => await cmd.ExecuteNonQueryAsync(ct));
   }

   public async Task<Column[]> GetColumnDefinitions(string table,CancellationToken ct, IDbTransaction? tx)
   {
      var sql = $"PRAGMA table_info({table})";
      return await ExecuteQuery(sql,tx, async command =>
      {
         await using var reader = await command.ExecuteReaderAsync(ct);
         var columnDefinitions = new List<Column>();
         while (await reader.ReadAsync(ct))
         {
            /*cid, name, type, notnull, dflt_value, pk */
            columnDefinitions.Add(new Column
           ( 
                Name : reader.GetString(1),
                Type : StringToDataType(reader.GetString(2))
            ));
         }
         return columnDefinitions.ToArray();
      });
   }
   
    private string DataTypeToString(string dataType)
    {
        return dataType switch
        {
            ColumnType.Int => "INTEGER",
            ColumnType.Text => "TEXT",
            ColumnType.Datetime => "INTEGER",
            ColumnType.String => "TEXT",
            _ => throw new NotSupportedException($"Type {dataType} is not supported")
        };
    }

    private string StringToDataType(string s)
    {
        s = s.ToLower();
        return s switch
        {
            "integer" => ColumnType.Int,
            _ => ColumnType.Text
        };
    }

    private async Task<T> ExecuteQuery<T>(string sql,IDbTransaction? tx, Func<SqliteCommand, Task<T>> executeFunc,
        params (string, object)[] parameters)
    {
        logger.LogInformation(sql);
        await using var command = new SqliteCommand(sql, connection);
        if (tx is not null)
            command.Transaction =
                tx as SqliteTransaction ?? throw new Exception("Transaction is not a Sqlite transaction");

        foreach (var (paramName, paramValue) in parameters)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }
        return await executeFunc(command);
    }
}