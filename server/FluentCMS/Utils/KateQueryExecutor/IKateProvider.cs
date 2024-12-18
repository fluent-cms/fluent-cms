using SqlKata.Execution;

namespace FluentCMS.Utils.KateQueryExecutor;

public record KateProviderOption(string ConnectionString);
public interface IKateProvider
{
    Task<T> Execute<T>(Func<QueryFactory, Task<T>> queryFunc);
}