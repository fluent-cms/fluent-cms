using FluentResults;

namespace FluentCMS.Modules;

public interface IAuthModule
{
    WebApplication UseCmsAuth(WebApplication app);
    Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role);
}