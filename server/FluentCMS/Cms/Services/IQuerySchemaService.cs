using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> GetByGraphFields(string entityName, IEnumerable<GraphQLField> fields, CancellationToken token = default);
    Task<LoadedQuery> GetByNameAndCache(string name, CancellationToken token);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
}