using FluentResults;

namespace FluentCMS.Auth.Services;

public sealed class UserDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public string[] ReadWriteEntities { get; set; } = [];
    public string[] RestrictedReadWriteEntities { get; set; } = [];
    public string[] ReadonlyEntities { get; set; } = [];
    public string[] RestrictedReadonlyEntities { get; set; } = [];
}

public sealed class RoleDto
{
    public string Name { get; set; } = "";
    public string[] ReadWriteEntities { get; set; } = [];
    public string[] RestrictedReadWriteEntities { get; set; } = [];
    public string[] ReadonlyEntities { get; set; } = [];
    public string[] RestrictedReadonlyEntities { get; set; } = [];
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