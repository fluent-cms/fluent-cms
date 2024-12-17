using FluentCMS.Auth.Services;
using FluentCMS.Utils.ResultExt;

namespace FluentCMS.Auth.Handlers;

public static class ProfileHandler
{
    public static void MapProfileHandlers(this RouteGroupBuilder app)
    {
        app.MapPost("/password", async (
            IProfileService svc, ProfileDto dto
        ) => await svc.ChangePassword(dto));

        app.MapGet("/info", (
            IProfileService svc
        ) => svc.GetInfo() ?? throw new ResultException("Unauthorized"));
    }
}