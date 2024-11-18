using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> GetByGraphFields(string entityName, IEnumerable<GraphQLField> fields,IEnumerable<IValueProvider> args);
    Task<LoadedQuery> GetByNameAndCache(string name, CancellationToken token);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
}