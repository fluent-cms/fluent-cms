namespace FluentCMS.Utils.Cache;

public interface ICacheProvider
{
    ValueTask<T> GetOrCreate<T>(string key, int ttlSec, Func<CancellationToken, ValueTask<T>> factory,  CancellationToken ct = default);
    ValueTask Remove(string key, CancellationToken ct = default);
}