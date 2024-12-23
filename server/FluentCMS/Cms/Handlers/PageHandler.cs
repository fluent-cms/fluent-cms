using FluentCMS.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Handlers;

public static class PageHandler
{
    public static RouteHandlerBuilder MapHomePage(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (
            IPageService pageService,
            HttpContext context,
            CancellationToken ct
        ) => await context.Html(await pageService.Get("home", context.Args(), ct), ct));
    }

    public static RouteGroupBuilder MapPages(this RouteGroupBuilder app)
    {
        app.MapGet("/page_part", async (
            IPageService pageService,
            HttpContext context,
            [FromQuery] string token,
            CancellationToken ct
        ) => await context.Html(await pageService.GetPart(token, ct), ct));

        app.MapGet("/{page}", async (
            IPageService pageService,
            HttpContext context,
            string page,
            CancellationToken ct
        ) => await context.Html(await pageService.Get(page, context.Args(), ct), ct));

        app.MapGet("/{page}/{slug}", async (
            IPageService pageService, 
            HttpContext context, 
            string page, 
            string slug, 
            CancellationToken ct
        ) => await context.Html(await pageService.GetDetail(page, slug, context.Args(), ct), ct));
        return app;
    }
}