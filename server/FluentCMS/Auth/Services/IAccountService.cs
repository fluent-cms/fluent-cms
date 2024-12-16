using FluentCMS.Auth.models;
using FluentResults;

namespace FluentCMS.Auth.Services;

public interface IAccountService
{
    Task<UserDto> GetOne(string id,CancellationToken cancellationToken);
    Task<UserDto[]> GetUsers(CancellationToken cancellationToken);
    Task<string[]> GetRoles(CancellationToken cancellationToken);
    Task<Result> EnsureUser(string email, string password, string[] roles);
    Task DeleteUser(string id);
    Task SaveUser(UserDto userDto);
    Task<RoleDto> GetOneRole(string id);
    Task SaveRole(RoleDto roleDto);
    Task DeleteRole(string name);
}