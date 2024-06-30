using Microsoft.Data.Sqlite;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace FluentCMS.Utils.Dao;

public class SqliteDao( string connectionString,bool debug):IDao
{
   private Compiler _compiler = new SqliteCompiler();
   public async Task<int?> Exec(Query? query)
   {
      if (query is null)
      {
         return null;
      }
      Log(query);
      return await ExecuteKateQuery(async db =>
      {
            
         var res =await db.ExecuteScalarAsync<int>(query); 
         return res;
      });
      
   }
   public async Task<Record?> One(Query? query)
   {
      Log(query);
      return query is null ? null : await ExecuteKateQuery(async db => await db.FirstOrDefaultAsync(query));
   }
   public async Task<Record[]?> Many(Query? query)
   {
      if (query is null)
      {
         return null;
      }
      Log(query);
      return await ExecuteKateQuery(async db =>
      {
         var items = await db.GetAsync(query);
         return items.Select((x => (Record)x)).ToArray();
      });
   }
   public async Task<int> Count(Query query)
   {
      Log(query);
      return await ExecuteKateQuery(async db => await db.CountAsync<int>(query));
   }
   
   private async Task<T> ExecuteKateQuery<T>(Func<QueryFactory, Task<T>> queryFunc)
   {
      await using var connection = new SqliteConnection(connectionString);
      await connection.OpenAsync();
      var db = new QueryFactory(connection, _compiler);
      return await queryFunc(db);
   }
   
   public async Task<ColumnDefinition[]> GetColumnDefinitions(string tableName)
   {
      var sql = $"PRAGMA table_info({tableName})";
      return await ExecuteQuery(sql, async command =>
      {
         await using var reader = command.ExecuteReader();
         var columnDefinitions = new List<ColumnDefinition>();
         while (await reader.ReadAsync())
         {
            var column = new ColumnDefinition
            {
               ColumnName = reader.GetString(0),
               MaxLength = reader.IsDBNull(2) ? "N/A" : reader.GetValue(2).ToString(),
               IsNullable = reader.GetString(3),
               DefaultValue = reader.IsDBNull(4) ? "N/A" : reader.GetValue(4).ToString()
            };
            var t = reader.GetString(1);
            switch (t)
            {
               case "integer":
                  column.DataType = DatabaseType.Int;
                  break;
               default:
                  column.DataType = DatabaseType.Text;
                  break;
            }
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
   private void Log(Query? query)
   {
      if (!debug || query is null)
      {
         return;
      }

      var res = _compiler.Compile(query);
      Console.WriteLine(res);
   }

}