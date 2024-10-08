using System.Security.Claims;
using FluentCMS.Services;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FluentCMS.Utils.IdentityExt;

namespace FluentCMS.Auth.Services;
using static InvalidParamExceptionFactory;
public  static class Roles
{
    /// <summary>
    /// super admin:
    /// schema: any schema,
    /// data: any entity
    /// </summary>
    public const string Sa = "sa"; 
    
    /// <summary>
    /// admin
    /// schema : only entity and view, only his own schema
    /// </summary>
    public const string Admin = "admin";
}
public class AccountService<TUser, TRole,TCtx>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager,
    IHttpContextAccessor accessor,
    TCtx context
) : IAccountService
    where TUser : IdentityUser, new()
    where TRole : IdentityRole, new()
    where TCtx : IdentityDbContext<TUser>

{
   
    public async Task<string[]> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await context.Roles.Select(x => x.Name??"").ToArrayAsync(cancellationToken);
        return roles;
    }

    public async Task<UserDto> GetOneUser(string id, CancellationToken cancellationToken)
    {
        var query = from user in context.Users
            where user.Id == id 
            join userRole in context.UserRoles
                on user.Id equals userRole.UserId into userRolesGroup
            from userRole in userRolesGroup.DefaultIfEmpty() // Left join for roles
            join role in context.Roles
                on userRole.RoleId equals role.Id into rolesGroup
            from role in rolesGroup.DefaultIfEmpty() // Left join for roles
            join userClaim in context.UserClaims
                on user.Id equals userClaim.UserId into userClaimsGroup
            from userClaim in userClaimsGroup.DefaultIfEmpty() // Left join for claims
            group new { role, userClaim } by user
            into userGroup
            select new { userGroup.Key, Values = userGroup.ToArray() };
        
        // use client calculation to support Sqlite
        var item = NotNull(await query.FirstOrDefaultAsync(cancellationToken))
            .ValOrThrow($"did not find user by id {id}");
        return new UserDto
        {
            Email = item.Key.Email!,
            Id = item.Key.Id,
            Roles = item.Values.Where(x=>x.role is not null).Select(x => x.role.Name!).Distinct().ToArray(),
            ReadWriteEntities = item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.FullAccess)
                .Select(x => x.userClaim.ClaimValue!).Distinct().ToArray(),
            RestrictedReadWriteEntities = item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedAccess)
                .Select(x => x.userClaim.ClaimValue!).Distinct().ToArray(),
            ReadonlyEntities = item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.FullRead)
                .Select(x => x.userClaim.ClaimValue!).Distinct().ToArray(),
            RestrictedReadonlyEntities = item.Values.Where(x => x.userClaim?.ClaimType == AccessScope.RestrictedRead)
                .Select(x => x.userClaim.ClaimValue!).Distinct().ToArray(),
        };
    }

    public async Task<UserDto[]> GetUsers(CancellationToken cancellationToken)
    {
        var query = from user in context.Users
            join userRole in context.UserRoles
                on user.Id equals userRole.UserId into userRolesGroup
            from userRole in userRolesGroup.DefaultIfEmpty() // Left join for roles
            join role in context.Roles
                on userRole.RoleId equals role.Id into rolesGroup
            from role in rolesGroup.DefaultIfEmpty() // Left join for roles
            group new { role } by user
            into userGroup
            select new {userGroup.Key, Roles =userGroup.ToArray()};
        var items = await query.ToArrayAsync(cancellationToken);
        // use client calculation to support Sqlite
        var dtos = items.Select(x => new UserDto
        {
            Email = x.Key.Email!,
            Id = x.Key.Id,
            Roles = x.Roles.Where(val=>val?.role is not null).Select(val => val.role.Name!).Distinct().ToArray()
        });
        return dtos.ToArray();
    }

    public async Task<Result> EnsureUser(string email, string password, string[] roles)
    {
        var result = await EnsureRoles(roles);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return Result.Ok();
        }

        user = new TUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
        };

        var res = await userManager.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            return Result.Fail(res.ErrorMessage());
        }

        res = await userManager.AddToRolesAsync(user, roles);
        return !res.Succeeded ? Result.Fail(res.ErrorMessage()) : Result.Ok();
    }

    public async Task DeleteUser(string id)
    {
        True(accessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only supper admin have permission");
        var user = NotNull(await userManager.Users.FirstOrDefaultAsync(x => x.Id == id))
            .ValOrThrow($"not find user by id {id}");
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidParamException(result.ErrorMessage());
        }
    }
    
   

    public async Task SaveUser(UserDto dto)
    {
        True(accessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only supper admin have permission");
        var user = await MustFindUser(dto.Id);
        var claims = await userManager.GetClaimsAsync(user);
        CheckResult(await AssignRole(user, dto.Roles));
        CheckResult(await AssignClaim(user, claims, AccessScope.FullAccess, dto.ReadWriteEntities));
        CheckResult(await AssignClaim(user, claims, AccessScope.RestrictedAccess, dto.RestrictedReadWriteEntities));
        CheckResult(await AssignClaim(user, claims, AccessScope.FullRead, dto.ReadonlyEntities));
        CheckResult(await AssignClaim(user, claims, AccessScope.RestrictedRead, dto.RestrictedReadonlyEntities));
    }

    private async Task<TUser> MustFindUser(string id)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        return NotNull(user).ValOrThrow($"not find user by id {id}");
    }

    private async Task<Result> AssignClaim(TUser user, IList<Claim> claims, string type, string[] values)
    {
        var currentValues = claims.Where(x => x.Type == type).Select(x => x.Value).ToArray();
        // Calculate roles to be removed and added
        var toRemove = currentValues.Except(values).ToArray();
        var toAdd = values.Except(currentValues).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        if (toRemove.Any())
        {
            var result = await userManager.RemoveClaimsAsync(user, toRemove.Select(x=>new Claim(type, x)));
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (toAdd.Any())
        {
            var result = await userManager.AddClaimsAsync(user, toAdd.Select(x=>new Claim(type, x)));
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
            }
        }
        return Result.Ok();
    }

    private async Task<Result> AssignRole(TUser user, string[] roles)
    {
        var currentRoles = await userManager.GetRolesAsync(user);

        // Calculate roles to be removed and added
        var rolesToRemove = currentRoles.Except(roles).ToArray();
        var rolesToAdd = roles.Except(currentRoles).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        if (rolesToRemove.Any())
        {
            var result = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
            }
        }

        // Add only the roles that are in the new roles but not in currentRoles
        if (rolesToAdd.Any())
        {
            var result = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!result.Succeeded)
            {
                return Result.Fail(result.ErrorMessage());
            }
        }
        return Result.Ok();
    }

    public async Task<RoleDto> GetOneRole(string name)
    {
        var role = NotNull(await roleManager.FindByNameAsync(name))
            .ValOrThrow($"not find role by id {name}");
        var claims = await roleManager.GetClaimsAsync(role);
        return new RoleDto
        {
            Name = name,
            ReadWriteEntities = claims.Where(x => x.Type == AccessScope.FullAccess).Select(x => x.Value).ToArray(),
            RestrictedReadWriteEntities =
                claims.Where(x => x.Type == AccessScope.RestrictedAccess).Select(x => x.Value).ToArray(),
            ReadonlyEntities = claims.Where(x => x.Type == AccessScope.FullRead).Select(x => x.Value).ToArray(),
            RestrictedReadonlyEntities = claims.Where(x => x.Type == AccessScope.RestrictedRead).Select(x => x.Value).ToArray()
        };
    }
    public async Task DeleteRole(string name)
    {
        if (name == Roles.Admin || name == Roles.Sa)
        {
            throw new InvalidParamException($"can not delete system role `{name}`");
        }
        
        True(accessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only supper admin have permission");
        var role = NotNull(await roleManager.FindByNameAsync(name))
            .ValOrThrow($"not find role by id {name}");
        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new InvalidParamException(result.ErrorMessage());
        }
    }

    public async Task SaveRole(RoleDto roleDto)
    {
        True(accessor.HttpContext.HasRole(Roles.Sa)).ThrowNotTrue("Only supper admin have permission");
        if (string.IsNullOrWhiteSpace(roleDto.Name))
        {
            throw new InvalidParamException("Role name can not be null");
        }
        CheckResult(await EnsureRoles([roleDto.Name]));
        var role = await roleManager.FindByNameAsync(roleDto.Name);
        var claims =await roleManager.GetClaimsAsync(role!);
        CheckResult(await AddClaimsToRole(role!, claims, AccessScope.FullAccess, roleDto.ReadWriteEntities));
        CheckResult(await AddClaimsToRole(role!, claims, AccessScope.RestrictedAccess, roleDto.RestrictedReadWriteEntities));
        CheckResult(await AddClaimsToRole(role!, claims, AccessScope.FullRead, roleDto.ReadonlyEntities));
        CheckResult(await AddClaimsToRole(role!, claims, AccessScope.RestrictedRead, roleDto.RestrictedReadonlyEntities));
    }

    private async Task<Result> AddClaimsToRole(TRole role,  IList<Claim> claims, string type, string[]values )
    {
        var currentValues = claims.Where(x => x.Type == type).Select(x => x.Value).ToArray();
        // Calculate roles to be removed and added
        var toRemove = currentValues.Except(values).ToArray();
        var toAdd = values.Except(currentValues).ToArray();

        // Remove only the roles that are in currentRoles but not in the new roles
        foreach (var claim in toRemove.Select(x=>new Claim(type,x)))
        {
            var identityResult = await roleManager.RemoveClaimAsync(role, claim);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.ErrorMessage());
            }
        }

        foreach (var claim in toAdd.Select(x => new Claim(type, x)))
        {
            var identityResult = await roleManager.AddClaimAsync(role, claim);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.ErrorMessage());
            }
        }
        return Result.Ok();
    }
    
    private async Task<Result> EnsureRoles(string[] roles)
    {
        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var res = await roleManager.CreateAsync(new TRole { Name = roleName });
            if (!res.Succeeded)
            {
                return Result.Fail(res.ErrorMessage());
            }
        }

        return Result.Ok();
    }
    
}