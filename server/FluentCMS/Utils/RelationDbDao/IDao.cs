using System.Data;
using SqlKata.Execution;

namespace FluentCMS.Utils.RelationDbDao;

public sealed record DatabaseTypeValue(string S = "", int? I = null, DateTime? D = null);

public interface IDao
{
    ValueTask<IDbTransaction> BeginTransaction();
    bool TryParseDataType(string s, string type, out DatabaseTypeValue? data);
    Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc);
    Task CreateTable(string table, IEnumerable<Column> cols, CancellationToken ct = default, IDbTransaction? tx = null);
    Task AddColumns(string table, IEnumerable<Column> cols, CancellationToken ct = default, IDbTransaction? tx = null);
    Task<Column[]> GetColumnDefinitions(string table, CancellationToken ct,IDbTransaction? tx = null);
}