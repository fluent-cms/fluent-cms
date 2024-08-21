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

    [HttpPost("roles")]
    public async Task SaveRoles(RoleDto dto)
    {
        await accountService.SaveRole(dto);
    }

    [HttpDelete("roles/{name}")]
    public async Task DeleteRole(string name)
    {
        await accountService.DeleteRole(name);
    }

    [HttpGet("roles/{name}")]
    public async Task<ActionResult<object[]>> GetOneRole(string name)
    {
        return Ok(await accountService.GetOneRole(name));
    }

    [HttpDelete("users/{id}")]
    public async Task DeleteUser(string id)
    {
        await accountService.DeleteUser(id);
    }

    [HttpPost("users")]
    public async Task SaveUser([FromBody] UserDto dto)
    {
        await accountService.SaveUser(dto);
    }
}