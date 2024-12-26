using System.Data;
using FluentCMS.Utils.RelationDbDao;
using SqlKata;

namespace FluentCMS.Utils.KateQueryExecutor;

public record KateQueryExecutorOption(int? Timeout);
public sealed class KateQueryExecutor(IDao provider, KateQueryExecutorOption option)
{
   public Task<int> Exec(
      Query query,  CancellationToken ct = default,IDbTransaction? tx = null
   ) => provider.Execute(db
      => db.ExecuteScalarAsync<int>(
         query: query,
         transaction:tx,
         timeout: option.Timeout,
         cancellationToken: ct)
   );

   public async Task<Record?> One(
      Query query, CancellationToken ct
   ) => await provider.Execute(db
      => db.FirstOrDefaultAsync(query: query, timeout: option.Timeout, cancellationToken: ct)
   );

   public Task<Record[]> Many(
      Query query, CancellationToken ct = default
   ) => provider.Execute(async db =>
   {
      var items = await db.GetAsync(query: query, timeout: option.Timeout, cancellationToken: ct);
      return items.Select(x => (Record)x).ToArray();
   });

   public async Task<int> Count(
      Query query, CancellationToken ct
   ) => await provider.Execute(db =>
      db.CountAsync<int>(query, timeout: option.Timeout, cancellationToken: ct));
}

  