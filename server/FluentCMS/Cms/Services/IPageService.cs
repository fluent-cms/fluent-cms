
namespace FluentCMS.Cms.Services;

public interface IPageService
{
    public Task<string> Get(string pageName, CancellationToken cancellationToken =default);
    public Task<string> GetBySlug(string pageName, string slug, CancellationToken cancellationToken = default);
}