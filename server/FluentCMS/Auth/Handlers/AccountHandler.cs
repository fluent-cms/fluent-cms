using FluentCMS.Auth.models;
using FluentCMS.Auth.Services;
using Microsoft.AspNetCore.Authorization;

namespace FluentCMS.Auth.Handlers;

public static class AccountHandlers
{
    public static void MapAccountHandlers(this RouteGroupBuilder app)
    {
        app.MapGet($"/users", async (IAccountService svc, CancellationToken ct) => await svc.GetUsers(ct));

        app.MapGet("/users/{id}", async (
            IAccountService svc, string id, CancellationToken ct
        ) => await svc.GetOne(id, ct));

        app.MapDelete("/users/{id}", async (IAccountService svc, string id) => await svc.DeleteUser(id));

        app.MapPost($"/users", async (IAccountService svc, UserDto dto) => await svc.SaveUser(dto));

        app.MapGet($"/roles", async (IAccountService svc, CancellationToken ct) => await svc.GetRoles(ct));

        app.MapGet("/roles/{name}", async (IAccountService svc, string name) => await svc.GetOneRole(name));

        app.MapPost("/roles", async (IAccountService svc, RoleDto dto) => await svc.SaveRole(dto));

        app.MapDelete("/roles/{name}", async (IAccountService svc, string name) => await svc.DeleteRole(name));
    }
}
