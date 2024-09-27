
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string pageName, Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken =default);
    Task<string> GetDetail(string pageName, string routerParamValue, Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken = default);
    Task<string> GetPartial(string partialToken, CancellationToken cancellationToken);
}