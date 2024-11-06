using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> List(string name, Span span, Pagination pagination, QueryArgs args, CancellationToken token);
    Task<Record[]> Many(string name,  QueryArgs args,  CancellationToken token);
    Task<Record> One(string name, QueryArgs args, CancellationToken token);
    Task<Record[]> Partial(string name, string attr, Span span, int limit, QueryArgs args, CancellationToken token);
}