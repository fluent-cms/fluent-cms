using FluentCMS.Models;
using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.WebAppExt;

public static class Auth
{
    public static void AddCmsAuth<TUser, TRole, TContext>(this WebApplicationBuilder builder)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : DbContext
    {
        builder.Services.AddIdentityApiEndpoints<TUser>().AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
        builder.Services.AddScoped<IUserService<TUser>,UserService<TUser,TRole>>();
        builder.Services.AddScoped<IPermissionService,PermissionService<TUser>>();
        builder.Services.AddHttpContextAccessor();
    }

    public static async Task<Result> EnsureCmsUser<TUser>(this WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetService<IUserService<TUser>>();
        return await userService?.EnsureUser(email, password, role)!;
    }

    public static void UseCmsAuth<TUser>(this WebApplication app)
        where TUser : IdentityUser, new()
    {
        using var scope = app.Services.CreateScope();
        app.UseAuthorization();
        app.MapGroup("/api").MapIdentityApi<TUser>();
        app.MapGet("/api/logout",
            async (SignInManager<TUser> signInManager) => await signInManager.SignOutAsync());


        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.AddHooks("*", [Occasion.BeforeSaveSchema, Occasion.BeforeDeleteSchema],
            async (IPermissionService service, Schema schema) =>
            {
                service.EnsureCreatedByField(schema);
                await service.CheckSchemaPermission(schema);
            });

        registry.AddHooks("*",
            [Occasion.BeforeAddRelated, Occasion.BeforeDeleteRelated, Occasion.BeforeDelete, Occasion.BeforeUpdate],
            async (IPermissionService service, RecordMeta meta) => await service.CheckEntityPermission(meta));

        registry.AddHooks("*", [Occasion.BeforeInsert],
            async (IPermissionService service, RecordMeta meta, Record record) =>
            {
                service.AssignCreatedBy(record);
                await service.CheckEntityPermission(meta);
            }
        );
    }
}