using FluentCMS.Utils.QueryBuilder;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> ListWithAction(string entityName, IEnumerable<GraphQLField> fields, IEnumerable<IInput> args);
    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, QueryStrArgs args, CancellationToken token);
    Task<Record> OneWithAction(string entityName, IEnumerable<GraphQLField> fields,IEnumerable<IInput> args);
    Task<Record> OneWithAction(string name, QueryStrArgs strArgs, CancellationToken token);
    Task<Record[]> Partial(string name, string attr, Span span, int limit, QueryStrArgs strArgs, CancellationToken token);
    Task<Record[]> ManyWithAction(string name,  QueryStrArgs strArgs,  CancellationToken token);
}