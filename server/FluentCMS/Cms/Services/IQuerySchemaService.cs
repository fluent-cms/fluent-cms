using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> GetByNameAndCache(string name, CancellationToken cancellationToken);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
}