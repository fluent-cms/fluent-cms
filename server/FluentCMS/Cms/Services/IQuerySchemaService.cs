using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest(Query query, IEnumerable<GraphQLField> fields, IDictionary<string, ArgumentValue> args);
    Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
}