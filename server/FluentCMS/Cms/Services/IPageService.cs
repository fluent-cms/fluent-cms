
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Cms.Services;

public interface IPageService
{
    public Task<string> Get(string pageName, Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken =default);
    public Task<string> GetDetail(string pageName, string routerParamValue, Dictionary<string,StringValues> qsDictionary, CancellationToken cancellationToken = default);
}