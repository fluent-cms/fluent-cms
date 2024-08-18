using FluentCMS.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    
    [HttpDelete("users")]
    public async Task<ActionResult<UserDto[]>> DeleteUser(UserDto dto, CancellationToken cancellationToken)
    {
        await accountService.SaveUser(dto, cancellationToken);
        return Ok();
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto[]>> SaveUser(UserDto dto, CancellationToken cancellationToken)
    {
        await accountService.SaveUser(dto, cancellationToken);
        return Ok();
    }

    [HttpGet("roles")]
    public async Task<ActionResult<object[]>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await accountService.GetRoles(cancellationToken));
    }
}