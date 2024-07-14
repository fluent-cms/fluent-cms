using SqlKata;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;

public interface IKateProvider
{
    Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc);
    SqlResult Compile(Query query);
}