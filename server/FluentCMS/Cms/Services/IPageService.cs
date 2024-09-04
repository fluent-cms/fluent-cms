using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;

public interface IPageService
{
    public Task<string> Get(string pageName, Cursor cursor, CancellationToken cancellationToken);
    public Task<string> GetBySlug(string pageName, string slug, Cursor cursor, CancellationToken cancellationToken);
}