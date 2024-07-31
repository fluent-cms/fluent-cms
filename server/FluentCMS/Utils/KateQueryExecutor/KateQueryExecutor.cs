using SqlKata;

namespace FluentCMS.Utils.KateQueryExecutor;

public sealed class KateQueryExecutor(IKateProvider provider)
{
   public async Task<int> Exec(Query query) =>
      await provider.Execute(async db => await db.ExecuteScalarAsync<int>(query));

   public async Task<Record?> One(Query query) =>
      await provider.Execute(async db => await db.FirstOrDefaultAsync(query));

   public async Task<Record[]> Many(Query query)
   {
      return await provider.Execute(async db =>
      {
         var items = await db.GetAsync(query);
         return items.Select(x => (Record)x).ToArray();
      });
   }

   public async Task<int> Count(Query query) => await provider.Execute(async db => await db.CountAsync<int>(query));
}

  