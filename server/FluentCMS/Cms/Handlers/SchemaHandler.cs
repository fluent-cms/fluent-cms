using FluentCMS.Cms.Models;
using FluentCMS.Cms.Services;
using FluentCMS.Types;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Cms.Handlers;

public static class SchemaHandler
{
    public static void MapSchemaHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/", async (
            ISchemaService svc, string? type, CancellationToken ct
        ) => await svc.AllWithAction(type ?? "", ct));

        app.MapPost("/", async (
            ISchemaService schemaSvc, IEntitySchemaService entitySchemaSvc, Schema dto, CancellationToken ct
        ) => dto.Type switch
        {
            SchemaType.Entity => await entitySchemaSvc.Save(dto, ct),
            _ => await schemaSvc.SaveWithAction(dto, ct)
        });

        app.MapGet("/{id}", async (
            ISchemaService svc, int id, CancellationToken ct
        ) => await svc.ByIdWithAction(id, ct) ?? throw new ResultException($"Cannot find schema {id}"));

        app.MapGet("/name/{name}", async (
                ISchemaService svc, string name, string type, CancellationToken ct
            ) => await svc.GetByNameDefault(name, type, ct) ??
                 throw new ResultException($"Cannot find schema {name} of type {type}"));

        app.MapDelete("/{id}", async (
            ISchemaService schemaSvc,
            IEntitySchemaService entitySchemaSvc,
            IQuerySchemaService querySchemaSvc,
            int id,
            CancellationToken ct
        ) =>
        {
            var schema = await schemaSvc.ById(id, ct);
            var task = schema?.Type switch
            {
                SchemaType.Entity => entitySchemaSvc.Delete(schema, ct),
                SchemaType.Query => querySchemaSvc.Delete(schema, ct),
                _ => schemaSvc.Delete(id, ct)
            };
            await task;
        });

        app.MapPost("/entity/define", async (
            IEntitySchemaService svc, Schema dto, CancellationToken ct
        ) => await svc.SaveTableDefine(dto, ct));

        app.MapGet("/entity/{table}/define", async (
            IEntitySchemaService svc, string table, CancellationToken ct
        ) => await svc.GetTableDefine(table, ct));

        app.MapGet("/entity/{name}", async (
            IEntitySchemaService service, string name, CancellationToken ct
        ) => (await service.GetLoadedEntity(name, ct)).Ok());

        app.MapPost("/entity/add_or_update", async (
            IEntitySchemaService svc,
            Entity entity,
            CancellationToken ct
        ) => await svc.AddOrUpdateByName(entity, ct));

        app.MapGet("/graphql", (
            IQuerySchemaService service
        ) => Results.Redirect(service.GraphQlClientUrl()));
    }
}