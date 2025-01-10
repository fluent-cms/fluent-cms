using FormCMS.Auth.DTO;
using FluentResults;

namespace FormCMS.Auth.Services;

public interface IAccountService
{
    Task<string[]> GetResources(CancellationToken ct);
    Task<UserDto> GetSingle(string id,CancellationToken ct);
    Task<UserDto[]> GetUsers(CancellationToken ct);
    Task<string[]> GetRoles(CancellationToken ct);
    Task<Result> EnsureUser(string email, string password, string[] roles);
    Task DeleteUser(string id);
    Task SaveUser(UserDto userDto);
    Task<RoleDto> GetSingleRole(string id);
    Task SaveRole(RoleDto roleDto);
    Task DeleteRole(string name);
}