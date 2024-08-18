using FluentCMS.Auth.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Auth.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ProfileController(IProfileService profileService):ControllerBase
{
    [HttpPost("password")]
    public async Task<ActionResult<ProfileDto[]>> ChangePassword(ProfileDto dto)
    {
        await profileService.ChangePassword(dto);
        return Ok();
    } 
}