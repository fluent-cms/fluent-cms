using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> Query(string entityName, IEnumerable<GraphQLField> fields);
    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, QueryArgs args, CancellationToken token);
    Task<Record[]> Many(string name,  QueryArgs args,  CancellationToken token);
    Task<Record> One(string name, QueryArgs args, CancellationToken token);
    Task<Record[]> Partial(string name, string attr, Span span, int limit, QueryArgs args, CancellationToken token);
}