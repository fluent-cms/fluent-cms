using FluentResults;

namespace FluentCMS.Services;

public interface IUserService<TUser>
{
    Task<List<TUser>> GetUsers();
    Task<Result> EnsureUser(string email, string password, string[] roles);
    Task<Result> DeleteUser(string id);
    Task<Result> AssignRole(string id, string[] roles);
}