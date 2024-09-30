
using FluentCMS.App;
using FluentCMS.Utils.HookFactory;


namespace FluentCMS.WebAppExt;
public class ContentRootConfig
{
    public string[] ContentRoots { get; set; } = [];
}

public static class Basic
{
    public static void AddPostgresCms(this WebApplicationBuilder builder, string connectionString, string staticAssetRootPath ="")
    {
        CmsApp.Build(builder, DatabaseProvider.Postgres,connectionString,staticAssetRootPath);
    }

    public static void AddSqliteCms(this WebApplicationBuilder builder, string connectionString, string staticAssetRootPath ="")
    {
        CmsApp.Build(builder, DatabaseProvider.Sqlite,connectionString, staticAssetRootPath);
    }

    public static void AddSqlServerCms(this WebApplicationBuilder builder, string connectionString, string staticAssetRootPath ="")
    {
        CmsApp.Build(builder, DatabaseProvider.SqlServer, connectionString, staticAssetRootPath);
    } 

    public static void RegisterCmsHook(this WebApplication app, string entityName, Occasion[] occasion, Delegate func)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.AddHooks(entityName, occasion, func);
    }

    public static async Task UseCmsAsync(this WebApplication app)
    {
        var cmsApp = app.Services.GetRequiredService<CmsApp>();
        await cmsApp.UseCmsAsync(app);
    }
}