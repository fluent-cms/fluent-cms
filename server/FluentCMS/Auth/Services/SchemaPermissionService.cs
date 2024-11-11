using System.Collections.Immutable;
using System.Security.Claims;
using FluentCMS.Auth.models;
using FluentCMS.Cms.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.IdentityExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Auth.Services;
using static InvalidParamExceptionFactory;


public class SchemaPermissionService<TUser>(
    IHttpContextAccessor contextAccessor,
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager,
    ISchemaService schemaService
) :ISchemaPermissionService
    where TUser : IdentityUser, new()

{
    public string[] GetAll()
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
        {
            return [];
        }

        if (contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            return contextAccessor.HttpContext!.User.Claims
                .Where(x=>x.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess or AccessScope.FullRead or AccessScope.RestrictedRead)
                .Select(x=>x.Value)
                .ToArray();
        }
        
        throw new InvalidParamException($"You don't have permission to access schemas");
    }

    public void GetOne(string schemaName)
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa) 
            || contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess,schemaName)
            || contextAccessor.HttpContext.HasClaims(AccessScope.FullRead,schemaName)
            || contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess,schemaName)
            || contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedRead,schemaName)
            )
        {
            return;
        }

        throw new InvalidParamException($"You don't have permission to access {schemaName}");
    }


    public async Task Delete(int schemaId)
    {
        var currentUserId = MustGetCurrentUserId();
        var find = NotNull(await schemaService.GetByIdDefault(schemaId)).ValOrThrow($"can not find schema");
        await CheckSchemaPermission(find, currentUserId);
    }

    public async Task<Schema> Save(Schema schema)
    {
        var currentUserId = MustGetCurrentUserId();
        await CheckSchemaPermission(schema, currentUserId);
        //create
        if (schema.Id == 0)
        {
            schema = schema with { CreatedBy = currentUserId };
            if (schema.Type == SchemaType.Entity)
            {
                await EnsureUserHaveAccessEntity(schema);
                schema = CheckResult(EnsureCreatedByField(schema));
            }
        }
        return schema;
    }

    private async Task CheckSchemaPermission(Schema schema, string currentUserId)
    {
        switch (schema.Type)
        {
            case SchemaType.Menu:
                True(contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
                    .ThrowNotTrue("Only Supper Admin has the permission to modify menu");
                break;
            default:
                await SaOrAdminHaveAccessToSchema(schema, currentUserId);
                break;
        }
    }

    private async Task EnsureUserHaveAccessEntity(Schema schema)
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
        {
            return;
        }

        //use have restricted access to the entity data
        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User);
        var claims = await userManager.GetClaimsAsync(user!);

        if (claims.FirstOrDefault(x =>
                x.Value == schema.Name && x.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess) == null)
        {
            await userManager.AddClaimAsync(user!, new Claim(AccessScope.RestrictedAccess, schema.Name));
        }

        await signInManager.RefreshSignInAsync(user!);
    }

    private static Result<Schema> EnsureCreatedByField(Schema schema)
    {
        var entity = schema.Settings.Entity;
        if (entity is null) return Result.Fail("Invalid Entity payload");
        if (schema.Settings.Entity?.Attributes.FindOneAttr(Constants.CreatedBy) is not null) return schema;

        ImmutableArray<Attribute> attributes =
        [
            ..entity.Attributes,
                new Attribute(Field: Constants.CreatedBy, Header: Constants.CreatedBy, DataType: DataType.String)
            
        ];
        return schema with{Settings = new Settings(Entity: entity with{Attributes = attributes})};
    }

    private async Task SaOrAdminHaveAccessToSchema(Schema schema, string currentUserId)
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
        {
            return;
        }

        if (!contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            throw new InvalidParamException("Only Admin and Super Admin can has this permission");
        }

        //modifying schema, make sure admin can only modify his own schema
        var isUpdate = schema.Id > 0;

        if (isUpdate)
        {
            var find = NotNull(await schemaService.GetByIdDefault(schema.Id)).ValOrThrow("not find schema");
            if (find.CreatedBy != currentUserId)
            {
                throw new InvalidParamException("You are not supper admin,  you can only change your own schema");
            }
        }
    }

    private string MustGetCurrentUserId() =>
        StrNotEmpty(contextAccessor.HttpContext.GetUserId()).ValOrThrow("not logged in");
}
  
