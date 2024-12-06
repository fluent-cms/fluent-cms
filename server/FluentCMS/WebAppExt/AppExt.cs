using FluentCMS.Modules;
using FluentCMS.Utils.HookFactory;
using FluentResults;

namespace FluentCMS.WebAppExt;

public static class AppExt
{
    public static async Task UseCmsAsync(this WebApplication app)
    {
        await app.Services.GetRequiredService<CmsModule>().UseCmsAsync(app);
        app.Services.GetService<IAuthModule>()?.UseCmsAuth(app);
    }

    public static HookRegistry GetHookRegistry(this WebApplication app) =>
        app.Services.GetRequiredService<HookRegistry>();

    public static async Task<Result> EnsureCmsUser(this WebApplication app, string email, string password,
        string[] role) =>
        await app.Services.GetRequiredService<IAuthModule>().EnsureCmsUser(app, email, password, role);

    public static void RegisterMongoViewHook(this WebApplication app, string viewName = "*") =>
        app.Services.GetRequiredService<MongoViewModule>().RegisterMongoViewHook(app, viewName);

    public static void RegisterMessageProducerHook(this WebApplication app, string entityName = "*") =>
        app.Services.GetRequiredService<EventProduceModule>().RegisterMessageProducerHook(app, entityName);

}