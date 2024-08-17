using System.Security.Claims;
using FluentCMS.Models;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Identity;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;
using static InvalidParamExceptionFactory;

public static class AccessScope
{
    public const string FullAccess = "FullAccess";
    public const string RestrictedAccess = "RestrictedAccess";
}

public class PermissionService<TUser>(
    IHttpContextAccessor contextAccessor, UserManager<TUser> userManager,
    ISchemaService schemaService, IEntityService entityService
    ):IPermissionService
    where TUser : IdentityUser, new()

{
    private const string CreatedBy = "created_by";
    public void AssignCreatedBy(Record record)
    {
        var currentUserId = MustGetCurrentUserId();
        record[CreatedBy] = currentUserId;
    } 
    
    public async Task CheckSchemaPermission(Schema schema)
    {
        var currentUserId = MustGetCurrentUserId();
        if (schema.Id > 0)
        {
            //read db to make sure the schema is faked by client
            schema = await schemaService.GetByIdVerify(schema.Id, false);
        }
        else
        {
            schema.CreatedBy = currentUserId;
        }
        
        switch (schema.Type)
        {
            case SchemaType.Menu:
                True(CurrentUserHasRole(Roles.Sa)).ThrowNotTrue("Only Supper Admin has the permission to modify menu");
                break;
            case SchemaType.Entity:
                CheckViewAndEntityPermission(schema,currentUserId);
                await EnsureUserHaveAccess(schema.Name);
                break;
            case SchemaType.View:
                CheckViewAndEntityPermission(schema,currentUserId);
                break;
        }
    }
    
    public async Task CheckEntityPermission(RecordMeta meta)
    {
        if (CurrentUserHasRole(Roles.Sa))
        {
            return;
        }

        if (await CurrentUserHasClaims(AccessScope.FullAccess, meta.Entity.Name))
        {
            return;
        }

        if (!await CurrentUserHasClaims(AccessScope.RestrictedAccess, meta.Entity.Name))
        {
            throw new InvalidParamException($"You don't have permission to [{meta.Entity.Name}]");
        }

        var isCreate = string.IsNullOrWhiteSpace(meta.Id);
        if (!isCreate)
        {
            //need to query database to get userId in case client fake data
            var currentUserId = MustGetCurrentUserId();
            var record = await entityService.OneByAttributes(meta.Entity.Name, meta.Id, [CreatedBy]);
            True(record.TryGetValue(CreatedBy, out var createdBy) && (string)createdBy == currentUserId)
                .ThrowNotTrue($"You can only access record created by you, entityName={meta.Entity.Name}, record id={meta.Id}");
        }
    }

    private string MustGetCurrentUserId()
    {
        var user = contextAccessor.HttpContext?.User;
        var id = user?.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidParamException("Can not find current user");
        }
        return id;
    }

    private bool CurrentUserHasRole(string role)
    {
        return contextAccessor.HttpContext?.User.IsInRole(role) == true;
    }
    private async Task EnsureUserHaveAccess(string schemaName)
    {
        //use have restricted access to the entity data
        if (CurrentUserHasRole(Roles.Sa))
        {
            return;
        }

        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User);
        var claims = await userManager.GetClaimsAsync(user!);
        
        if (claims.FirstOrDefault(x=>x.Value == schemaName && x.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess) == null)
        {
            await userManager.AddClaimAsync(user!, new Claim(AccessScope.RestrictedAccess, schemaName));
        }
    }
    public void EnsureCreatedByField(Schema schema)
    {
        var entity = schema.Settings.Entity;
        if (entity is null) return;
        if (schema.Settings.Entity?.FindOneAttribute(CreatedBy) is not null) return;

        entity.Attributes = entity.Attributes.Append(new Attribute
        {
            Field = CreatedBy,
            DataType = DataType.String,
        }).ToArray();
    }

    private void CheckViewAndEntityPermission(Schema schema, string currentUserId)
    {
        if (CurrentUserHasRole(Roles.Sa))
        {
            return;
        }

        if (!CurrentUserHasRole(Roles.Admin))
        {
            throw new InvalidParamException("Only Admin and Super Admin can has this permission");
        }

        //modifying schema, make sure admin can only modify his own schema
        var isUpdate = schema.Id > 0;
        if (isUpdate && schema.CreatedBy != currentUserId)
        {
            throw new InvalidParamException("You are not supper admin,  you can only change your own schema");
        }
    }
    private async Task<bool> CurrentUserHasClaims(string claimType, string value)
    {
        var userClaims = contextAccessor.HttpContext?.User;
        if (userClaims?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (userClaims.Claims.FirstOrDefault(x => x.Value == value && x.Type == claimType) != null)
        {
            return true;
        }
        
        //check db incase if user haven't gotten chance to logout and login, 
        var user = await userManager.GetUserAsync(userClaims);
        var claims = await userManager.GetClaimsAsync(user!);
        return claims.FirstOrDefault(x => x.Value == value && x.Type == claimType) != null;
    }
}