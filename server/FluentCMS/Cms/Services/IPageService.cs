
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string name, Dictionary<string,StringValues> args, CancellationToken token =default);
    Task<string> GetDetail(string name, string param, Dictionary<string,StringValues> args, CancellationToken token = default);
    Task<string> GetPartial(string patialToken, CancellationToken token);
}