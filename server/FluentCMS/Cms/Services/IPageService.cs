
namespace FluentCMS.Cms.Services;

public interface IPageService
{
    public Task<string> Get(string pageName, CancellationToken cancellationToken =default);
    public Task<string> GetDetail(string pageName, string key, CancellationToken cancellationToken = default);
}