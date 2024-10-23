using FluentCMS.Auth.Services;
using FluentCMS.Utils.HookFactory;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.Modules;

public sealed class AuthModule<TCmsUser>(ILogger<IAuthModule> logger) : IAuthModule
    where TCmsUser : IdentityUser, new()
{
    public static void AddCmsAuth<TUser, TRole, TContext>(WebApplicationBuilder builder)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {

        builder.Services.AddSingleton<IAuthModule, AuthModule<TUser>>(); 

        builder.Services.AddIdentityApiEndpoints<TUser>().AddRoles<TRole>().AddEntityFrameworkStores<TContext>();
        builder.Services.AddScoped<IAccountService, AccountService<TUser, TRole, TContext>>();
        builder.Services.AddScoped<ISchemaPermissionService, SchemaPermissionService<TUser>>();
        builder.Services.AddScoped<IEntityPermissionService, EntityPermissionService>();
        builder.Services.AddScoped<IProfileService, ProfileService<TUser>>();
        builder.Services.AddHttpContextAccessor();
    }

    public void UseCmsAuth(WebApplication app)
    {
        logger.LogInformation("using cms auth");
        app.UseAuthorization();
        app.MapGroup("/api").MapIdentityApi<TCmsUser>();
        app.MapGet("/api/logout", async (SignInManager<TCmsUser> signInManager) => await signInManager.SignOutAsync());

        var registry = app.Services.GetRequiredService<CmsModule>().GetHookRegistry(app);

        registry.SchemaPreSave.RegisterDynamic("*",
            async (ISchemaPermissionService schemaPermissionService, SchemaPreSaveArgs args) => args with
            {
                RefSchema = await schemaPermissionService.Save(args.RefSchema)
            });

        registry.SchemaPreDel.RegisterDynamic("*",
            async (ISchemaPermissionService schemaPermissionService, SchemaPreDelArgs args) =>
            {
                await schemaPermissionService.Delete(args.SchemaId);
                return args;
            });

        registry.SchemaPreGetAll.RegisterDynamic("*",
            (ISchemaPermissionService schemaPermissionService, SchemaPreGetAllArgs args) =>
                args with { OutSchemaNames = schemaPermissionService.GetAll() });

        registry.SchemaPostGetOne.RegisterDynamic("*",
            (ISchemaPermissionService schemaPermissionService, SchemaPostGetOneArgs args) =>
            {
                schemaPermissionService.GetOne(args.Schema.Name);
                return args;
            });

        
        registry.EntityPreGetOne.RegisterDynamic("*", 
            (IEntityPermissionService service, EntityPreGetOneArgs args) =>
            {
                service.GetOne(args.Name, args.RecordId);
                return args;
            });

        registry.EntityPreGetList.RegisterDynamic("*", 
            (IEntityPermissionService service, EntityPreGetListArgs args) =>
                args with {RefFilters = service.List(args.Name, args.RefFilters)});
                
        registry.CrosstablePreAdd.RegisterDynamic("*",
            async (IEntityPermissionService service, CrosstablePreAddArgs args ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

        registry.CrosstablePreDel.RegisterDynamic("*",
            async (IEntityPermissionService service, CrosstablePreDelArgs args ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });
        
        registry.EntityPreDel.RegisterDynamic("*",
            async (IEntityPermissionService service, EntityPreDelArgs args ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });
        
        registry.EntityPreUpdate.RegisterDynamic("*",
            async (IEntityPermissionService service, EntityPreUpdateArgs args ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });
        
       
        registry.EntityPreAdd.RegisterDynamic("*",
            (IEntityPermissionService service, EntityPreAddArgs args) =>
            {
                service.Create(args.Name );
                service.AssignCreatedBy(args.RefRecord);
                return args;
            }
        );
    }
    
    public async Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountService>().EnsureUser(email, password,role);
    }
}