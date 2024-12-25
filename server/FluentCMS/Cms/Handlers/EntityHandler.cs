using System.Text.Json;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Cms.Handlers;

public static class EntityHandler
{
    public static void MapEntityHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/{name}", async (
            IEntityService entityService,
            HttpContext context,
            string name,
            [FromQuery] string? offset,
            [FromQuery] string? limit,
            [FromQuery] ListResponseMode? mode,
            CancellationToken ct
        ) => await entityService.ListWithAction(
            name,
            mode?? ListResponseMode.all,
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
            [FromBody] JsonElement ele,
            CancellationToken ct
        ) => await entityService.InsertWithAction(name, ele, ct));

        app.MapPost("/{name}/update", async (
            IEntityService entityService,
            string name,
            [FromBody] JsonElement ele,
            CancellationToken ct
        ) => await entityService.UpdateWithAction(name, ele, ct));

        app.MapPost("/{name}/delete", async (
            IEntityService entityService,
            string name,
            [FromBody] JsonElement ele,
            CancellationToken ct
        ) => await entityService.DeleteWithAction(name, ele, ct));

        app.MapPost("/{entityName}/{id}/{attributeName}/delete", async (
            IEntityService entityService,
            string entityName,
            string id,
            string attributeName,
            [FromBody] JsonElement[] items,
            CancellationToken ct
        ) => await entityService.JunctionDelete(entityName, id, attributeName, items, ct));

        app.MapPost("/{name}/{id}/{attr}/save", async (
            IEntityService entityService,
            string name,
            string id,
            string attr,
            JsonElement[] elements,
            CancellationToken ct
        ) => await entityService.JunctionAdd(name, id, attr, elements, ct));

        app.MapGet("/{name}/{id}/{attr}", async (
            IEntityService entityService,
            HttpContext context,
            string name,
            string id,
            string attr,
            [FromQuery] string? offset,
            [FromQuery] string? limit,
            bool exclude,
            CancellationToken ct
        ) => await entityService.JunctionList(
            name, id, attr, exclude,
            context.Args(),
            new Pagination(offset, limit),
            ct));

        app.MapGet("lookup/{name}", async (
            IEntityService entityService,
            string name,
            [FromQuery] string? query,
            CancellationToken ct
        ) => await entityService.LookupList(name, query ?? "", ct));
    }
}