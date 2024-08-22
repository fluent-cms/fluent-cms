using FluentCMS.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Auth.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ProfileController(IProfileService profileService):ControllerBase
{
    [HttpPost("password")]
    public async Task ChangePassword(ProfileDto dto)
    {
        await profileService.ChangePassword(dto);
    }

    [HttpGet("info")]
    public UserDto GetInfo()
    {
        return profileService.GetInfo();
    }
}