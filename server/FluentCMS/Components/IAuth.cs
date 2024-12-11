using FluentResults;

namespace FluentCMS.Components;

public interface IAuth
{
    WebApplication UseCmsAuth(WebApplication app);
    Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role);
}