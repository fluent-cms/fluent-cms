using System.Security.Claims;
using FluentCMS.Services;
using Microsoft.AspNetCore.Identity;

namespace FluentCMS.Auth.Services;
using static InvalidParamExceptionFactory;

public sealed class ProfileDto
{
    public string OldPassword { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ProfileService<TUser>(
    UserManager<TUser> userManager,
    IHttpContextAccessor contextAccessor 
    ):IProfileService
where TUser :IdentityUser, new()
{
    public UserDto GetInfo()
    {
        
        var claims = contextAccessor.HttpContext?.User;
        True(claims?.Identity?.IsAuthenticated == true)
            .ThrowNotTrue("Not logged in");
        return new UserDto
        {
            Email = claims?.FindFirstValue(ClaimTypes.Email) ?? "",
            Roles = claims?.FindAll(ClaimTypes.Role).Select(x=>x.Value).ToArray()??[],
            FullAccessEntities = claims?.FindAll(AccessScope.FullAccess).Select(x=>x.Value).ToArray()??[],
            RestrictedAccessEntities = claims?.FindAll(AccessScope.RestrictedAccess).Select(x=>x.Value).ToArray()??[],
        };
    }
    
    public async Task ChangePassword(ProfileDto dto)
    {
        var user = await MustGetCurrentUser();
        var result =await userManager.ChangePasswordAsync(user, dto.OldPassword, dto.Password);
        True(result.Succeeded).ThrowNotTrue(IdentityErrMsg(result));
    }

    private async Task<TUser> MustGetCurrentUser()
    {
        var claims = contextAccessor.HttpContext?.User;
        True(claims?.Identity?.IsAuthenticated == true)
            .ThrowNotTrue("Not logged in");
        var user =await userManager.GetUserAsync(claims!);
        return NotNull(user).ValOrThrow("Not logged in");
    }
    private static string IdentityErrMsg(IdentityResult result) =>  string.Join("\r\n", result.Errors.Select(e => e.Description));

}