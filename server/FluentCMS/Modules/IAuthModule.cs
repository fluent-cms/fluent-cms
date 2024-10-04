using FluentResults;

namespace FluentCMS.Modules;

public interface IAuthModule
{
    void UseCmsAuth(WebApplication app);
    Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role);
}