using System.Security.Claims;
using FluentCMS.Cms.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.HookFactory;
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
    public const string FullRead = "FullRead";
    public const string RestrictedRead = "RestrictedRead";
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

    public async Task HandleDeleteSchema(SchemaMeta schemaMeta)
    {
        var currentUserId = MustGetCurrentUserId();
        var find = await schemaService.GetByIdAndVerify(schemaMeta.SchemaId, false);
        await CheckSchemaPermission(find, currentUserId);
    }

    public async Task HandleSaveSchema(Schema schema)
    {
        var currentUserId = MustGetCurrentUserId();
        //edit
        if (schema.Id > 0)
        {
            await CheckSchemaPermission(schema, currentUserId);
        }
        //create
        else
        {
            await CheckSchemaPermission(schema, currentUserId);
            schema.CreatedBy = currentUserId;
            if (schema.Type == SchemaType.Entity)
            {
                await EnsureUserHaveAccess(schema);
                EnsureCreatedByField(schema);
            }
        }
    }

    public  void CheckEntityReadPermission(EntityMeta meta, Filters filters)
    {
         if (contextAccessor.HttpContext.HasRole(Roles.Sa))
         {
             return;
         }
 
         if (contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, meta.EntityName)
             || contextAccessor.HttpContext.HasClaims(AccessScope.FullRead,meta.EntityName))
         {
             return;
         }
         
         if (!(contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, meta.EntityName)
               || contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedRead, meta.EntityName)))
         {
             throw new InvalidParamException($"You don't have permission to read [{meta.EntityName}]");
         }
         
         filters.Add(new Filter
         {
             FieldName = CreatedBy ,
             Constraints = [new Constraint
             {
                 Match = Matches.EqualsTo,
                 ResolvedValues = [MustGetCurrentUserId()],
             }],
         });
       
    }
    public async Task CheckEntityAccessPermission(EntityMeta meta)
    {
        if (contextAccessor.HttpContext.HasRole(Roles.Sa))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, meta.EntityName))
        {
            return;
        }

        if (!contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, meta.EntityName))
        {
            throw new InvalidParamException($"You don't have permission to save [{meta.EntityName}]");
        }

        var isCreate = string.IsNullOrWhiteSpace(meta.RecordId);
        if (!isCreate)
        {
            //need to query database to get userId in case client fake data
            var record = await entityService.OneByAttributes(meta.EntityName, meta.RecordId, [CreatedBy]);
            True(record.TryGetValue(CreatedBy, out var createdBy) && (string)createdBy == MustGetCurrentUserId())
                .ThrowNotTrue($"You can only access record created by you, entityName={meta.EntityName}, record id={meta.RecordId}");
        }
    }
    private async Task CheckSchemaPermission(Schema schema, string currentUserId)
    {
        switch (schema.Type)
        {
            case SchemaType.Menu:
                True(contextAccessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only Supper Admin has the permission to modify menu");
                break;
            case SchemaType.Entity:
                await CheckQueryAndEntityPermission(schema,currentUserId);
                break;
            case SchemaType.Query:
                await CheckQueryAndEntityPermission(schema,currentUserId);
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
        if (schema.Settings.Entity?.Attributes.FindOneAttribute(CreatedBy) is not null) return;

        entity.Attributes = entity.Attributes.Append(new Attribute
        {
            Field = CreatedBy,
            Header = CreatedBy,
            DataType = DataType.String,
        }).ToArray();
    }

    private async Task CheckQueryAndEntityPermission(Schema schema, string currentUserId)
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
        
        if (isUpdate )
        {
            var find = await schemaService.GetByIdAndVerify(schema.Id, false);
            if (find.CreatedBy != currentUserId)
            {
                throw new InvalidParamException("You are not supper admin,  you can only change your own schema");
            }
        }
    }
    
    private string MustGetCurrentUserId() => StrNotEmpty(contextAccessor.HttpContext.GetUserId()).ValOrThrow("not logged int"); 
  
}