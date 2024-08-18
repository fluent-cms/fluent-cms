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
    
    public async Task ChangePassword(ProfileDto dto)
    {
        var user = await MustGetCurrentUser();
        var result =await userManager.ChangePasswordAsync(user, dto.OldPassword, dto.Password);
        True(result.Succeeded).ThrowNotTrue(IdentityErrMsg(result));
    }

    private async Task<TUser> MustGetCurrentUser()
    {
        var clainms = contextAccessor.HttpContext?.User;
        True(clainms?.Identity?.IsAuthenticated == true)
            .ThrowNotTrue("Not logged in");
        var user =await userManager.GetUserAsync(clainms!);
        return NotNull(user).ValOrThrow("Not lgged in");
    }
    private static string IdentityErrMsg(IdentityResult result) =>  string.Join("\r\n", result.Errors.Select(e => e.Description));

}