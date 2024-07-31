using SqlKata;
using SqlKata.Execution;

namespace FluentCMS.Utils.KateQueryExecutor;

public interface IKateProvider
{
    Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc);
}