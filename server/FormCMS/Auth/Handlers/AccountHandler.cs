using FormCMS.Auth.DTO;
using FormCMS.Auth.Services;

namespace FormCMS.Auth.Handlers;

public static class AccountHandlers
{
    public static void MapAccountHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/users", (IAccountService svc, CancellationToken ct) => svc.GetUsers(ct));

        app.MapGet("/users/{id}", (IAccountService svc, string id, CancellationToken ct) => svc.GetSingle(id, ct));

        app.MapDelete("/users/{id}", (IAccountService svc, string id) => svc.DeleteUser(id));

        app.MapPost("/users", (IAccountService svc, UserDto dto) => svc.SaveUser(dto));

        app.MapGet("/roles", (IAccountService svc, CancellationToken ct) => svc.GetRoles(ct));

        app.MapGet("/roles/{name}", (IAccountService svc, string name) => svc.GetSingleRole(name));

        app.MapPost("/roles", (IAccountService svc, RoleDto dto) => svc.SaveRole(dto));

        app.MapDelete("/roles/{name}", (IAccountService svc, string name) => svc.DeleteRole(name));
        
        app.MapGet("/resources",(IAccountService svc, CancellationToken ct) => svc.GetResources(ct));
    }
}
