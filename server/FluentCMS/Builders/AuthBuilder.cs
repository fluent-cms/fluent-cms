using FluentCMS.Auth.Handlers;
using FluentCMS.Auth.Services;
using FluentCMS.Utils.HookFactory;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.Builders;

public sealed class AuthBuilder<TCmsUser> (ILogger<AuthBuilder<TCmsUser>> logger): IAuthBuilder
    where TCmsUser : IdentityUser, new()
{
    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(IServiceCollection services)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        services.AddSingleton<IAuthBuilder, AuthBuilder<TUser>>();
        services.AddIdentityApiEndpoints<TUser>()
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
        services.AddScoped<IAccountService, AccountService<TUser, TRole, TContext>>();
        services.AddScoped<ISchemaPermissionService, SchemaPermissionService<TUser>>();
        services.AddScoped<IEntityPermissionService, EntityPermissionService>();
        services.AddScoped<IProfileService, ProfileService<TUser>>();
        services.AddHttpContextAccessor();
        return services;
    }

    public WebApplication UseCmsAuth(WebApplication app)
    {
        logger.LogInformation(
            """
            *********************************************************
            Using CMS Auth API endpoints
            *********************************************************
            """);
        MapEndpoints();
        RegisterHooks();

        return app;
        
        
        void MapEndpoints()
        {
            var cmsOptions = app.Services.GetRequiredService<CmsBuilder>().Options;
            var apiGroup = app.MapGroup(cmsOptions.RouteOptions.ApiBaseUrl);
            apiGroup.MapIdentityApi<TCmsUser>();
            apiGroup.MapGroup("/accounts").MapAccountHandlers();
            apiGroup.MapGet("/logout", async (
                SignInManager<TCmsUser> signInManager
            ) => await signInManager.SignOutAsync());
        }

        void RegisterHooks()
        {
            var registry = app.Services.GetRequiredService<HookRegistry>();

            registry.SchemaPreSave.RegisterDynamic("*", async (
                ISchemaPermissionService schemaPermissionService, SchemaPreSaveArgs args
            ) => args with
            {
                RefSchema = await schemaPermissionService.Save(args.RefSchema)
            });

            registry.SchemaPreDel.RegisterDynamic("*", async (
                ISchemaPermissionService schemaPermissionService, SchemaPreDelArgs args
            ) =>
            {
                await schemaPermissionService.Delete(args.SchemaId);
                return args;
            });

            registry.SchemaPreGetAll.RegisterDynamic("*", (
                ISchemaPermissionService schemaPermissionService, SchemaPreGetAllArgs args
            ) => args with { OutSchemaNames = schemaPermissionService.GetAll() });

            registry.SchemaPostGetOne.RegisterDynamic("*", (
                ISchemaPermissionService schemaPermissionService, SchemaPostGetOneArgs args
            ) =>
            {
                schemaPermissionService.GetOne(args.Schema);
                return args;
            });

            registry.EntityPreGetOne.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreGetOneArgs args
            ) =>
            {
                service.GetOne(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreGetList.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreGetListArgs args
            ) => args with { RefFilters = service.List(args.Name, args.Entity, args.RefFilters) });

            registry.CrosstablePreAdd.RegisterDynamic("*", async (
                IEntityPermissionService service, CrosstablePreAddArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.CrosstablePreDel.RegisterDynamic("*", async (
                IEntityPermissionService service, CrosstablePreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreDel.RegisterDynamic("*", async (
                IEntityPermissionService service, EntityPreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreUpdate.RegisterDynamic("*", async (
                IEntityPermissionService service, EntityPreUpdateArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreAdd.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreAddArgs args
            ) =>
            {
                service.Create(args.Name);
                service.AssignCreatedBy(args.RefRecord);
                return args;
            });
        }
    }

    public async Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountService>().EnsureUser(email, password,role);
    }
}