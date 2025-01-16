using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Auth.DTO;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.ApiClient;

public class AccountApiClient(HttpClient client)
{
   

    public async Task<Result<string[]>> GetEntities()
        =>await client.GetResult<string[]>("/api/accounts/entities" ,JsonOptions.IgnoreCase);
    public async Task<Result<string[]>> GetRoles()
        =>await client.GetResult<string[]>("/api/accounts/roles" ,JsonOptions.IgnoreCase);
    public async Task<Result<UserDto[]>> GetUsers()
        =>await client.GetResult<UserDto[]>("/api/accounts/users" ,JsonOptions.IgnoreCase);
    public async Task<Result<UserDto>> GetSingleUsers(string userId)
        =>await client.GetResult<UserDto>($"/api/accounts/users/{userId}" ,JsonOptions.IgnoreCase);
    public async Task<Result> DeleteUser(string userId)
        =>await client.DeleteResult($"/api/accounts/users/{userId}");

    public async Task<Result> SaveUser(UserDto userDto)
        => await client.PostResult($"/api/accounts/users", userDto,JsonOptions.IgnoreCase);
    
    public async Task<Result> SaveRole(RoleDto roleDto)
        => await client.PostResult($"/api/accounts/roles", roleDto,JsonOptions.IgnoreCase);
    
    public async Task<Result<RoleDto>> GetRole(string role)
        => await client.GetResult<RoleDto>($"/api/accounts/roles/{role}", JsonOptions.IgnoreCase);
    public async Task<Result> DeleteRole(string role)
        => await client.DeleteResult($"/api/accounts/roles/{role}");
}