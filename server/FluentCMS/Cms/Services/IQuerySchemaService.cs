using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest<T>(string entityName, IEnumerable<GraphQLField> fields, T[] args)
        where T : IObjectProvider, IPairProvider, IValueProvider;
    Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
}