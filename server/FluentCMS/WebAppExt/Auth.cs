using FluentCMS.Auth.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.WebAppExt;

public static class Auth
{
    public static void AddCmsAuth<TUser, TRole, TContext>(this WebApplicationBuilder builder)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        builder.Services.AddIdentityApiEndpoints<TUser>().AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
        
        builder.Services.AddScoped<IAccountService,AccountService<TUser,TRole,TContext>>();
        builder.Services.AddScoped<IPermissionService,PermissionService<TUser>>();
        builder.Services.AddScoped<IProfileService,ProfileService<TUser>>();
        builder.Services.AddHttpContextAccessor();
    }

    public static async Task<Result> EnsureCmsUser(this WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAccountService>();
        return await service?.EnsureUser(email, password, role)!;
    }

    public static void UseCmsAuth<TUser>(this WebApplication app)
        where TUser : IdentityUser, new()
    {
        using var scope = app.Services.CreateScope();
        app.UseAuthorization();
        app.MapGroup("/api").MapIdentityApi<TUser>();
        app.MapGet("/api/logout", async (SignInManager<TUser> signInManager) => await signInManager.SignOutAsync());

        var registry = app.Services.GetRequiredService<HookRegistry>();
        
        registry.AddHooks("*", [Occasion.BeforeSaveSchema],
            async (IPermissionService service, Schema schema) => await service.HandleSaveSchema(schema));

        registry.AddHooks("*", [Occasion.BeforeDeleteSchema],
            async (IPermissionService service, SchemaMeta meta) => await service.HandleDeleteSchema(meta));
        
        registry.AddHooks("*", [Occasion.BeforeReadOne, Occasion.BeforeReadList],
            (IPermissionService service, EntityMeta meta, Filters filters) => service.CheckEntityReadPermission(meta, filters));

        registry.AddHooks("*",
            [Occasion.BeforeAddRelated, Occasion.BeforeDeleteRelated, Occasion.BeforeDelete, Occasion.BeforeUpdate],
            async (IPermissionService service, EntityMeta meta) => await service.CheckEntityAccessPermission(meta));

        registry.AddHooks("*", [Occasion.BeforeInsert],
            async (IPermissionService service, EntityMeta meta, Record record) =>
            {
                service.AssignCreatedBy(record);
                await service.CheckEntityAccessPermission(meta);
            }
        );
    }

}