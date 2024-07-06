using SqlKata;
using SqlKata.Execution;

namespace Utils.KateQueryExecutor;

public interface IKateProvider
{
    Task<T> Execute<T>(Query? query, Func<QueryFactory, Task<T>> queryFunc);
}