using FluentCMS.Utils.Graph;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Validation;

namespace FluentCMS.Cms.Services;


public interface IQueryService
{
    Task<Record[]> ListWithAction(GraphQlRequestDto dto);

    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args,
        CancellationToken token);

    Task<Record?> OneWithAction(GraphQlRequestDto dto);

    Task<Record?> OneWithAction(string name, StrArgs strArgs, CancellationToken token);

    Task<Record[]> Partial(string name, string attr, Span span, int limit, StrArgs strArgs,
        CancellationToken token);

    Task<Record[]> ManyWithAction(string name, StrArgs strArgs, CancellationToken token);
}