using FluentCMS.Utils.QueryBuilder;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> ListWithAction(Query query, 
        IEnumerable<GraphQLField> fields,
        IDictionary<string, ArgumentValue> args);

    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, QueryStrArgs args,
        CancellationToken token);

    Task<Record?> OneWithAction(Query query, 
        IEnumerable<GraphQLField> fields,
        IDictionary<string, ArgumentValue> args);

    Task<Record?> OneWithAction(string name, QueryStrArgs strArgs, CancellationToken token);

    Task<Record[]> Partial(string name, string attr, Span span, int limit, QueryStrArgs strArgs,
        CancellationToken token);

    Task<Record[]> ManyWithAction(string name, QueryStrArgs strArgs, CancellationToken token);
}