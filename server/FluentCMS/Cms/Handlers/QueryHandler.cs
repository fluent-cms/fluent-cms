using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Handlers;

public static class QueryHandlers
{
    public static RouteGroupBuilder MapQueryHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/{name}", async (
            IQueryService svc,
            HttpContext ctx,
            string name,
            string? first,
            string? last,
            string? offset,
            string? limit,
            CancellationToken ct
        ) => await svc.ListWithAction(name, new Span(first, last), new Pagination(offset, limit), ctx.Args(), ct));

        app.MapGet("/{name}/single", async (
            IQueryService queryService,
            HttpContext httpContext,
            string name,
            CancellationToken token
        ) => await queryService.SingleWithAction(name, httpContext.Args(), token));

        app.MapGet("/{name}/part/{attr}", async (
            IQueryService svc,
            HttpContext ctx,
            string name,
            string attr,
            string? first,
            string? last,
            int limit,
            CancellationToken token
        ) => await svc.Partial(name, attr, new Span(first, last), limit, ctx.Args(), token));
        return app;
    }
}