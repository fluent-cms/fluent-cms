using System.Text.Json;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Handlers;

public static class EntityHandler
{
    public static void MapEntityHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/{name}", async (
            IEntityService entityService,
            HttpContext context,
            string name,
            string? offset,
            string? limit,
            ListResponseMode? mode,
            CancellationToken ct
        ) => await entityService.ListWithAction(
            name,
            mode ?? ListResponseMode.all,
            new Pagination(offset, limit),
            context.Args(),
            ct));

        app.MapGet("/{name}/{id}", async (
            IEntityService entityService,
            string name,
            string id,
            CancellationToken ct
        ) => await entityService.SingleWithAction(name, id, ct));

        app.MapPost("/{name}/insert", async (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) => await entityService.InsertWithAction(name, ele, ct));

        app.MapPost("/{name}/update", async (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) => await entityService.UpdateWithAction(name, ele, ct));

        app.MapPost("/{name}/delete", async (
            IEntityService entityService,
            string name,
            JsonElement ele,
            CancellationToken ct
        ) => await entityService.DeleteWithAction(name, ele, ct));

        app.MapPost("/junction/{name}/{id}/{attributeName}/delete", async (
            IEntityService entityService,
            string name,
            string id,
            string attributeName,
            JsonElement[] items,
            CancellationToken ct
        ) => await entityService.JunctionDelete(name, id, attributeName, items, ct));

        app.MapPost("/junction/{name}/{id}/{attr}/save", async (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            JsonElement[] elements,
            CancellationToken ct
        ) => await entityService.JunctionSave(name, id, attr, elements, ct));

        app.MapGet("/junction/{name}/{id}/{attr}", async (
            IEntityService entityService,
            HttpContext context,
            string name,
            string id,
            string attr,
            string? offset,
            string? limit,
            bool exclude,
            CancellationToken ct
        ) => await entityService.JunctionList(
            name, id, attr, exclude,
            new Pagination(offset, limit),
            context.Args(),
            ct));

        app.MapGet("/collection/{name}/{id}/{attr}", async (
            IEntityService entityService,
            HttpContext context,
            string name,
            string id,
            string attr,
            string? offset,
            string? limit,
            CancellationToken ct
        ) => await entityService.CollectionList(
            name, id, attr,
            new Pagination(offset, limit),
            context.Args(),
            ct));

        app.MapPost("/collection/{name}/{id}/{attr}/insert", async (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            JsonElement element,
            CancellationToken ct
        ) => await entityService.CollectionInsert(name, id, attr, element, ct));

        app.MapGet("/lookup/{name}", async (
            IEntityService entityService,
            string name,
            string? query,
            CancellationToken ct
        ) => await entityService.LookupList(name, query ?? "", ct));
    }
}