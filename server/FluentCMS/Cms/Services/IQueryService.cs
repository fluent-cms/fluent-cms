using FluentCMS.Graph;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Validation;

namespace FluentCMS.Cms.Services;


public interface IQueryService
{
    Task<Record[]> ListWithAction(GraphQlRequestDto dto);

    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args,
        CancellationToken token);

    Task<Record?> SingleWithAction(GraphQlRequestDto dto);

    Task<Record?> SingleWithAction(string name, StrArgs args, CancellationToken token);

    Task<Record[]> Partial(string name, string attr, Span span, int limit, StrArgs args,
        CancellationToken token);
}