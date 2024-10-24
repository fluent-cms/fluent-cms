using System.Collections.Immutable;
using FluentCMS.Auth.models;
using FluentCMS.Cms.Services;
using FluentCMS.Services;
using FluentCMS.Utils.IdentityExt;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Auth.Services;
using static InvalidParamExceptionFactory;

public class EntityPermissionService(    
    IHttpContextAccessor contextAccessor ,
    IEntityService entityService

):IEntityPermissionService
{
    public ImmutableArray<ValidFilter> List(string entityName, ImmutableArray<ValidFilter> filters)
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa))
        {
            return filters;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName)
            || contextAccessor.HttpContext.HasClaims(AccessScope.FullRead, entityName))
        {
            return filters;
        }

        if (!(contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)
              || contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedRead, entityName)))
        {
            throw new InvalidParamException($"You don't have permission to read [{entityName}]");
        }

        return
        [
            ..filters,
            new ValidFilter(Constants.CreatedBy, "and",
                [new ValidConstraint(Matches.EqualsTo, [MustGetCurrentUserId()])])
        ];
    }

    public async Task GetOne(string entityName, string recordId)
    {
        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullRead,entityName))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)||
            contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName))
        {
            string[] attrs = [Constants.CreatedBy];
            //need to query database to get userId in case client fake data
            var record = await entityService.OneByAttributes(entityName, recordId, attrs);
            if (record.TryGetValue(Constants.CreatedBy, out var createdBy) && (string)createdBy == MustGetCurrentUserId())
            {
                return;
            }
            throw new InvalidParamException(
                $"You can only access record created by you, entityName={entityName}, record id={recordId}");
        }

        throw new InvalidParamException($"You don't have permission to save [{entityName}]");
    }

    public void Create(string entityName)
    {
        if (!(contextAccessor.HttpContext.HasRole(RoleConstants.Sa) || contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName)))
        {
            throw new InvalidParamException($"You don't have permission to save [{entityName}]");
        }
    }

    public async Task Change(string entityName, string recordId)
    {

        if (contextAccessor.HttpContext.HasRole(RoleConstants.Sa) ||
            contextAccessor.HttpContext.HasClaims(AccessScope.FullAccess, entityName))
        {
            return;
        }

        if (contextAccessor.HttpContext.HasClaims(AccessScope.RestrictedAccess, entityName))
        {
            string[] attrs = [Constants.CreatedBy];
            //need to query database to get userId in case client fake data
            var record = await entityService.OneByAttributes(entityName, recordId, attrs);
            if (record.TryGetValue(Constants.CreatedBy, out var createdBy) && (string)createdBy == MustGetCurrentUserId())
            {
                return;
            }
            throw new InvalidParamException(
                    $"You can only access record created by you, entityName={entityName}, record id={recordId}");
        }

        throw new InvalidParamException($"You don't have permission to save [{entityName}]");
    }


    public void AssignCreatedBy(Record record)
    {
        record[Constants.CreatedBy] = MustGetCurrentUserId();
    }
    private string MustGetCurrentUserId() =>
        StrNotEmpty(contextAccessor.HttpContext.GetUserId()).ValOrThrow("not logged in");
}