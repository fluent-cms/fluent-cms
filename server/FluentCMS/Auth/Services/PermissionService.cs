using System.Security.Claims;
using FluentCMS.Cms.Services;
using FluentCMS.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.IdentityExt;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Identity;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Auth.Services;
using static InvalidParamExceptionFactory;
public static class AccessScope
{
    public const string FullAccess = "FullAccess";
    public const string RestrictedAccess = "RestrictedAccess";
}
public class PermissionService<TUser>(
    IHttpContextAccessor contextAccessor, 
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager,
    ISchemaService schemaService, IEntityService entityService
    ):IPermissionService
    where TUser : IdentityUser, new()

{
    private const string CreatedBy = "created_by";
    public void AssignCreatedBy(Record record)
    {
        record[CreatedBy] = MustGetCurrentUserId();
    }
    public async Task HandleSchema(Schema schema)
    {
        var currentUserId = MustGetCurrentUserId();
        if (schema.Id > 0)
        {
            //read db to make sure the schema is faked by client
            var find = await schemaService.GetByIdAndVerify(schema.Id, false);
            CheckSchemaPermission(find, currentUserId);
            schema.CreatedBy = find.CreatedBy;
        }
        else
        {
            CheckSchemaPermission(schema, currentUserId);
            schema.CreatedBy = currentUserId;
        }

        if (schema.Type == SchemaType.Entity)
        {
            await EnsureUserHaveAccess(schema);
            EnsureCreatedByField(schema);
        }
    }
    public async Task CheckEntity(RecordMeta meta)
    {
        if (contextAccessor.HttpContext.HasRole(Roles.Sa))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, meta.Entity.Name))
        {
            return;
        }

        if (!contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, meta.Entity.Name))
        {
            throw new InvalidParamException($"You don't have permission to [{meta.Entity.Name}]");
        }

        var isCreate = string.IsNullOrWhiteSpace(meta.Id);
        if (!isCreate)
        {
            //need to query database to get userId in case client fake data
            var record = await entityService.OneByAttributes(meta.Entity.Name, meta.Id, [CreatedBy]);
            True(record.TryGetValue(CreatedBy, out var createdBy) && (string)createdBy == MustGetCurrentUserId())
                .ThrowNotTrue($"You can only access record created by you, entityName={meta.Entity.Name}, record id={meta.Id}");
        }
    }
    private void CheckSchemaPermission(Schema schema, string currentUserId)
    {
        switch (schema.Type)
        {
            case SchemaType.Menu:
                True(contextAccessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only Supper Admin has the permission to modify menu");
                break;
            case SchemaType.Entity:
                CheckViewAndEntityPermission(schema,currentUserId);
                break;
            case SchemaType.View:
                CheckViewAndEntityPermission(schema,currentUserId);
                break;
        }
    } 
    private async Task EnsureUserHaveAccess(Schema schema)
    {
        //use have restricted access to the entity data
        if (contextAccessor.HttpContext.HasRole(Roles.Sa))
        {
            return;
        }

        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User);
        var claims = await userManager.GetClaimsAsync(user!);
        
        if (claims.FirstOrDefault(x=>x.Value == schema.Name && x.Type is AccessScope.RestrictedAccess or AccessScope.FullAccess) == null)
        {
            await userManager.AddClaimAsync(user!, new Claim(AccessScope.RestrictedAccess, schema.Name));
        }

        await signInManager.RefreshSignInAsync(user!);
    }
    private void EnsureCreatedByField(Schema schema)
    {
        var entity = schema.Settings.Entity;
        if (entity is null) return;
        if (schema.Settings.Entity?.FindOneAttribute(CreatedBy) is not null) return;

        entity.Attributes = entity.Attributes.Append(new Attribute
        {
            Field = CreatedBy,
            Header = CreatedBy,
            DataType = DataType.String,
        }).ToArray();
    }

    private void CheckViewAndEntityPermission(Schema schema, string currentUserId)
    {
        if (contextAccessor.HttpContext.HasRole(Roles.Sa))
        {
            return;
        }

        if (!contextAccessor.HttpContext.HasRole(Roles.Admin))
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
    
    private string MustGetCurrentUserId() => StrNotEmpty(contextAccessor.HttpContext.GetUserId()).ValOrThrow("not logged int"); 
  
}