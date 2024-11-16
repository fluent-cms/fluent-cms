using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> ListWithAction(string entityName, IEnumerable<GraphQLField> fields);
    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, QueryArgs args, CancellationToken token);
    Task<Record[]> ManyWithAction(string name,  QueryArgs args,  CancellationToken token);
    Task<Record> OneWithAction(string entityName, IEnumerable<GraphQLField> fields);
    Task<Record> OneWithAction(string name, QueryArgs args, CancellationToken token);
    Task<Record[]> Partial(string name, string attr, Span span, int limit, QueryArgs args, CancellationToken token);
}