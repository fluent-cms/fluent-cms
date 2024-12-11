using System.Collections.Immutable;
using System.Security.Claims;
using FluentCMS.Auth.models;
using FluentCMS.Cms.Services;
using FluentCMS.Cms.Models;
using FluentCMS.Exceptions;
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
        if (!contextAccessor.HttpContext.HasRole(RoleConstants.Sa) &&
            !contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            throw new InvalidParamException($"Fail to get schema list, you don't have [Sa] or [Admin] role.");
        }
        return [];
    }

    public void GetOne(Schema schema)
    {
        if (!contextAccessor.HttpContext.HasRole(RoleConstants.Sa) &&
            !contextAccessor.HttpContext.HasRole(RoleConstants.Admin))
        {
            throw new InvalidParamException($"You don't have permission to access {schema.Type}:{schema.Name}");
        }
    }

    public async Task Delete(int schemaId)
    {
        var find = NotNull(await schemaService.ById(schemaId)).ValOrThrow($"can not find schema by id ${schemaId}");
        await EnsureWritePermissionAsync(find);
    }

    public async Task<Schema> Save(Schema schema)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new InvalidParamException($"You are not logged in, can not save schema {schema.Type} [{schema.Name}]");
        }
        await EnsureWritePermissionAsync(schema);
        
        if (schema.Id != 0) return schema;
        
        schema = schema with { CreatedBy = userId};
        if (schema.Type != SchemaType.Entity) return schema;
        
        await EnsureCurrentUserHaveEntityAccess(schema);
        schema = Ok(EnsureSchemaHaveCreatedByField(schema));
        return schema;
    }

    private async Task EnsureWritePermissionAsync(Schema schema)
    {
        if (contextAccessor.HttpContext?.User.Identity?.IsAuthenticated == false)
        {
            throw new InvalidParamException($"You are not logged in, can not save {schema.Type} [{schema.Name}]");
        }
        var hasPermission = schema.Type switch
        {
            SchemaType.Menu => contextAccessor.HttpContext.HasRole(RoleConstants.Sa),
            _ when schema.Id == 0 => 
                contextAccessor.HttpContext.HasRole(RoleConstants.Admin) || 
                contextAccessor.HttpContext.HasRole(RoleConstants.Sa),
            _ => 
                contextAccessor.HttpContext.HasRole(RoleConstants.Sa) || 
                await IsCreatedByCurrentUser(schema)
        };

        if (!hasPermission)
        {
            throw new InvalidParamException($"You don't have permission to save {schema.Type} [{schema.Name}]");
        }
    }

    private async Task EnsureCurrentUserHaveEntityAccess(Schema schema)
    {
        var user = await userManager.GetUserAsync(contextAccessor.HttpContext!.User);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        var claims = await userManager.GetClaimsAsync(user);

        var hasAccess = claims.Any(claim => 
            claim.Value == schema.Name && 
            (claim.Type == AccessScope.RestrictedAccess || claim.Type == AccessScope.FullAccess)
        );

        if (!hasAccess)
        {
            await userManager.AddClaimAsync(user, new Claim(AccessScope.RestrictedAccess, schema.Name));
            await signInManager.RefreshSignInAsync(user);
        }
    }

    private static Result<Schema> EnsureSchemaHaveCreatedByField(Schema schema)
    {
        var entity = schema.Settings.Entity;
        if (entity is null) return Result.Fail("can not ensure schema have created_by field, invalid Entity payload");
        if (schema.Settings.Entity?.Attributes.FindOneAttr(Constants.CreatedBy) is not null) return schema;

        ImmutableArray<Attribute> attributes =
        [
            ..entity.Attributes,
            new(Field: Constants.CreatedBy, Header: Constants.CreatedBy, DataType: DataType.String)
        ];
        return schema with{Settings = new Settings(Entity: entity with{Attributes = attributes})};
    }

    private async Task<bool> IsCreatedByCurrentUser(Schema schema)
    {
        if (!contextAccessor.HttpContext.GetUserId(out var userId))
        {
            throw new InvalidParamException("Can not verify schema is created by you, you are not logged in.");
        }
        var find = NotNull(await schemaService.ById(schema.Id))
            .ValOrThrow($"Can not verify schema is created by you, can not find schema by id [{schema.Id}]");
        return find.CreatedBy == userId;
    }
}
  
