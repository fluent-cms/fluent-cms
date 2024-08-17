using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace FluentCMS.Services;
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

public class UserService<TUser, TRole>(
    UserManager<TUser> userManager,
    RoleManager<TRole> roleManager
) : IUserService<TUser>
    where TUser : IdentityUser, new()
    where TRole : IdentityRole, new()
{
    public async Task<List<TUser>> GetUsers()
    {
        return await userManager.Users.ToListAsync();
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
            return Result.Fail(string.Join(",", res.Errors.Select(e=>e.Description)));
        }

        res = await userManager.AddToRolesAsync(user, roles);
        return !res.Succeeded ? Result.Fail(string.Join(",", res.Errors.Select(e => e.Description))) : Result.Ok();
    }

    public async Task<Result> DeleteUser(string id)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return Result.Fail($"not find user by id {id}");
        }

        var result = await userManager.DeleteAsync(user);
        return !result.Succeeded ? Result.Fail(string.Join(",", result.Errors)) : Result.Ok();
    }

    public async Task<Result> AssignRole(string id, string[] roles)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return Result.Fail($"not find user by id {id}");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var result = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!result.Succeeded)
        {
            return Result.Fail(string.Join("\r\n", result.Errors));
        }

        result = await userManager.AddToRolesAsync(user, roles);
        return !result.Succeeded
            ? Result.Fail(string.Join("\r\n", result.Errors.Select(e => e.Description)))
            : Result.Ok();
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
                return Result.Fail(string.Join(",", res.Errors.Select(e => e.Description)));
            }
        }

        return Result.Ok();
    }
}