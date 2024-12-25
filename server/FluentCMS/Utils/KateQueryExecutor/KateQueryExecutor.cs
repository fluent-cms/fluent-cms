using SqlKata;

namespace FluentCMS.Utils.KateQueryExecutor;

public record KateQueryExecutorOption(int? Timeout);
public sealed class KateQueryExecutor(IKateProvider provider, KateQueryExecutorOption option)
{
   public async Task<int> Exec(Query query, CancellationToken cancellationToken =default)
   {
      return await provider.Execute(async db =>
         await db.ExecuteScalarAsync<int>(query: query, timeout: option.Timeout, cancellationToken: cancellationToken));
   }

   public async Task<Record?> One(Query query, CancellationToken cancellationToken )
   {
      return await provider.Execute(async db =>
         await db.FirstOrDefaultAsync(query: query, timeout: option.Timeout, cancellationToken: cancellationToken));
   }

   public async Task<Record[]> Many(Query query,  CancellationToken cancellationToken =default)
   {
      return await provider.Execute(async db =>
      {
         var items = await db.GetAsync(query: query, timeout: option.Timeout, cancellationToken: cancellationToken);
         return items.Select(x => (Record)x).ToArray();
      });
   }

   public async Task<int> Count(Query query, CancellationToken cancellationToken)
   {
      return await provider.Execute(async db =>
         await db.CountAsync<int>(query, timeout: option.Timeout, cancellationToken: cancellationToken));
   } 
}

  