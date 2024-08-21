using FluentResults;

namespace FluentCMS.Auth.Services;

public sealed class UserDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public string[] FullAccessEntities { get; set; } = [];
    public string[] RestrictedAccessEntities { get; set; } = [];
}

public sealed class RoleDto
{
    public string Name { get; set; } = "";
    public string[] FullAccessEntities { get; set; } = [];
    public string[] RestrictedAccessEntities { get; set; } = [];
}

public interface IAccountService
{
    Task<UserDto> GetOneUser(string id,CancellationToken cancellationToken);
    Task<UserDto[]> GetUsers(CancellationToken cancellationToken);
    Task<string[]> GetRoles(CancellationToken cancellationToken);
    Task<Result> EnsureUser(string email, string password, string[] roles);
    Task DeleteUser(string id);
    Task SaveUser(UserDto userDto);
    Task<RoleDto> GetOneRole(string id);
    Task SaveRole(RoleDto roleDto);
    Task DeleteRole(string name);
}