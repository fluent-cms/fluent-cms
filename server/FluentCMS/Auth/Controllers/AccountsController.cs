using FluentCMS.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FluentCMS.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Sa},{Roles.Admin}")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<UserDto[]>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await accountService.GetUsers(cancellationToken));
    }
    
    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserDto[]>> GetOneUser(string id,CancellationToken cancellationToken)
    {
        return Ok(await accountService.GetOneUser(id,cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<object[]>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await accountService.GetRoles(cancellationToken));
    }

    [HttpDelete("users/{id}")]
    public async Task<ActionResult<UserDto[]>> DeleteUser(string id)
    {
        await accountService.DeleteUser(id);
        return Ok();
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto[]>> SaveUser([FromBody] UserDto dto)
    {
        await accountService.SaveUser(dto);
        return Ok();
    }
}