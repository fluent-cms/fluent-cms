using Microsoft.Data.Sqlite;
using SqlKata;

namespace Utils.Dao;

public sealed class SqliteDao(string connectionString, bool debug) : IDao
{
   private readonly SqliteDaoUtil _util = new(connectionString, debug);

   public async Task<int?> Exec(Query? query)
   {
      return query is null
         ? null
         : await _util.ExecuteKateQuery(query, async db =>
         {
            var res = await db.ExecuteScalarAsync<int>(query);
            return res;
         });
   }

   public async Task<Record?> One(Query? query)
   {
      return query is null
         ? null
         : await _util.ExecuteKateQuery(query, async db => await db.FirstOrDefaultAsync(query));
   }

   public async Task<Record[]?> Many(Query? query)
   {
      return query is null
         ? null
         : await _util.ExecuteKateQuery(query, async db =>
         {
            var items = await db.GetAsync(query);
            return items.Select(x => (Record)x).ToArray();
         });
   }

   public async Task<int> Count(Query? query)
   {
      return await _util.ExecuteKateQuery(query, async db => await db.CountAsync<int>(query));
   }

   public async Task CreateTable(string tableName, ColumnDefinition[] columnDefinitions)
   {
      var sql = _util.GenerateCreateTableSql(tableName, columnDefinitions);
      await _util.ExecuteQuery(sql, async cmd => await cmd.ExecuteNonQueryAsync());
   }
   
   public async Task AddColumns(string tableName, ColumnDefinition[] columnDefinitions)
   {
      if (columnDefinitions.Length == 0)
      {
         return;
      }
      var sqlStrs = _util.GenerateAddColumnSql(tableName, columnDefinitions);
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
      return await _util.ExecuteQuery(sql, async command =>
      {
         await using var reader = await command.ExecuteReaderAsync();
         var columnDefinitions = new List<ColumnDefinition>();
         while (await reader.ReadAsync())
         {
            columnDefinitions.Add(_util.CreateColumnDefinition(reader.GetString(1), reader.GetString(2)));
         }

         return columnDefinitions.ToArray();
      }, ("tableName", tableName));
   }
}