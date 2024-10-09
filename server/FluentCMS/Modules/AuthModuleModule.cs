using FluentCMS.Auth.Services;
using FluentCMS.Utils.HookFactory;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FluentCMS.Modules;

public sealed class AuthModuleModule<TCmsUser>(ILogger<IAuthModule> logger) : IAuthModule
    where TCmsUser : IdentityUser, new()
{
    public static void AddCmsAuth<TUser, TRole, TContext>(WebApplicationBuilder builder)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {

        builder.Services.AddSingleton<IAuthModule, AuthModuleModule<TUser>>(); 

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
            async (ISchemaPermissionService schemaPermissionService, SchemaPreSaveArgs parameter) =>
            {
                await schemaPermissionService.Save(parameter.RefSchema);
            });

        registry.SchemaPreDel.RegisterDynamic("*",
            async (ISchemaPermissionService schemaPermissionService, SchemaPreDelArgs parameter) =>
            {
                await schemaPermissionService.Delete(parameter.SchemaId);
            });

        registry.SchemaPreGetAll.RegisterDynamic("*",
            (ISchemaPermissionService schemaPermissionService, SchemaPreGetAllArgs args) => args with { OutSchemaNames = schemaPermissionService.GetAll() });

        registry.SchemaPostGetOne.RegisterDynamic("*",
            (ISchemaPermissionService schemaPermissionService, SchemaPostGetOneArgs parameter) =>
            {
                schemaPermissionService.GetOne(parameter.Name);
                return parameter;
            });

        
        registry.EntityPreGetOne.RegisterDynamic("*", 
            (IEntityPermissionService service, EntityPreGetOneArgs parameter) =>
            {
                service.GetOne(parameter.Name, parameter.RecordId);
                return parameter;
            });

        registry.EntityPreGetList.RegisterDynamic("*", 
            (IEntityPermissionService service, EntityPreGetListArgs parameter) =>
            {
                service.List(parameter.Name, parameter.RefFilters);
                return parameter;
            });
                
        registry.CrosstablePreAdd.RegisterDynamic("*",
            async (IEntityPermissionService service, CrosstablePreAddArgs parameter ) =>
            {
                await service.Change(parameter.Name, parameter.RecordId);
                return parameter;
            });

        registry.CrosstablePreDel.RegisterDynamic("*",
            async (IEntityPermissionService service, CrosstablePreDelArgs parameter ) =>
            {
                await service.Change(parameter.Name, parameter.RecordId);
                return parameter;
            });
        
        registry.EntityPreDel.RegisterDynamic("*",
            async (IEntityPermissionService service, EntityPreDelArgs parameter ) =>
            {
                await service.Change(parameter.Name, parameter.RecordId);
                return parameter;
            });
        
        registry.EntityPreUpdate.RegisterDynamic("*",
            async (IEntityPermissionService service, EntityPreUpdateArgs parameter ) =>
            {
                await service.Change(parameter.Name, parameter.RecordId);
                return parameter;
            });
        
       
        registry.EntityPreAdd.RegisterDynamic("*",
            (IEntityPermissionService service, EntityPreAddArgs parameter) =>
            {
                service.Create(parameter.Name );
                service.AssignCreatedBy(parameter.RefRecord);
                return parameter;
            }
        );
    }
    
    public async Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountService>().EnsureUser(email, password,role);
    }
}