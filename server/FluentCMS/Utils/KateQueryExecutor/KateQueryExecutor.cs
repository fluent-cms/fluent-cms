using SqlKata;

namespace FluentCMS.Utils.KateQueryExecutor;

public sealed class KateQueryExecutor(IKateProvider provider, int? timeout)
{
   public async Task<int> Exec(Query query, CancellationToken cancellationToken )
   {
      return await provider.Execute(async db =>
         await db.ExecuteScalarAsync<int>(query: query, timeout: timeout, cancellationToken: cancellationToken));
   }

   public async Task<Record?> One(Query query, CancellationToken cancellationToken )
   {
      return await provider.Execute(async db =>
         await db.FirstOrDefaultAsync(query: query, timeout: timeout, cancellationToken: cancellationToken));
   }

   public async Task<Record[]> Many(Query query,  CancellationToken cancellationToken )
   {
      return await provider.Execute(async db =>
      {
         var items = await db.GetAsync(query: query, timeout: timeout, cancellationToken: cancellationToken);
         return items.Select(x => (Record)x).ToArray();
      });
   }

   public async Task<int> Count(Query query, CancellationToken cancellationToken)
   {
      return await provider.Execute(async db =>
         await db.CountAsync<int>(query, timeout: timeout, cancellationToken: cancellationToken));
   } 
}

  