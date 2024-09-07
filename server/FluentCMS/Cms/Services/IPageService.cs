
namespace FluentCMS.Cms.Services;

public interface IPageService
{
    public Task<string> Get(string pageName, CancellationToken cancellationToken =default);
    public Task<string> GetByRouterKey(string pageName, string key, CancellationToken cancellationToken = default);
}