using SqlKata;

namespace Utils.KateQueryExecutor;

public sealed class KateQueryExecutor(IKateProvider provider)
{
   public async Task<int> Exec(Query? query)
   {
      return query is null
         ? 0
         : await provider.Execute(async db =>
         {
            var res = await db.ExecuteScalarAsync<int>(query);
            return res;
         });
   }

   public async Task<Record?> One(Query? query)
   {
      return query is null
         ? null
         : await provider.Execute(async db => await db.FirstOrDefaultAsync(query));
   }

   public async Task<Record[]> Many(Query query)
   {
      return await provider.Execute(async db =>
         {
            var items = await db.GetAsync(query);
            return items.Select(x => (Record)x).ToArray();
         });
   }

   public async Task<int> Count(Query? query)
   {
      return await provider.Execute(async db => await db.CountAsync<int>(query));
   }
}

  