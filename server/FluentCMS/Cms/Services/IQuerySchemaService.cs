using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields);
    Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token);
    Task Delete(Schema schema, CancellationToken token);
    Task SaveQuery(Query query, CancellationToken ct = default);
    string GraphQlClientUrl();

}