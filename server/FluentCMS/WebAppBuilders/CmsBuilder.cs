using System.Collections.Immutable;
using System.Reflection;
using FluentCMS.Auth.Handlers;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Handlers;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Graph;
using FluentCMS.Types;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using GraphQL;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Rewrite;
using Npgsql;
using Schema = FluentCMS.Graph.Schema;

namespace FluentCMS.WebAppBuilders;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public sealed record Problem(string Title, string? Detail =null);

public sealed record DbOption(DatabaseProvider Provider, string ConnectionString);
public sealed class CmsBuilder(
    CmsOptions cmsOptions,
    DbOption dbOptions,
    ILogger<CmsBuilder> logger
)
{
    private const string FluentCmsContentRoot = "/_content/FluentCMS";
    public CmsOptions Options => cmsOptions;

    public static IServiceCollection AddCms(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        Action<CmsOptions>? optionsAction = null)
    {
        var cmsOptions = new CmsOptions();
        optionsAction?.Invoke(cmsOptions);

        services.AddSingleton(cmsOptions);
        services.AddSingleton(new DbOption(databaseProvider, connectionString));
        services.AddSingleton<CmsBuilder>();
        services.AddSingleton<HookRegistry>();
        services.AddScoped<IProfileService, DummyProfileService>();
        
        AddDbServices();
        AddCacheServices();
        AddStorageServices();
        AddGraphqlServices();
        AddPageTemplateServices();
        AddCmsServices();
        
        return services;

        void AddCmsServices()
        {
            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();

            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageService, PageService>();

        }

        void AddPageTemplateServices()
        {
            services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FluentCmsContentRoot}/static-assets/templates/template.html");
                return new PageTemplate(new PageTemplateConfig(fileInfo.PhysicalPath!));
            });
        }

        void AddGraphqlServices()
        {
            // init for each request, make sure get the latest entity definition
            services.AddScoped<Schema>();
            services.AddScoped<GraphQuery>();
            services.AddScoped<DateClause>();
            services.AddScoped<Clause>();
            services.AddScoped<StringClause>();
            services.AddScoped<IntClause>();
            services.AddScoped<MatchTypeEnum>();
            services.AddScoped<SortOrderEnum>();
            services.AddScoped<FilterExpr>();
            services.AddScoped<SortExpr>();

            services.AddGraphQL(b =>
            {
                b.AddSystemTextJson();
                b.AddUnhandledExceptionHandler(ex =>
                {
                    if (ex.Exception is ResultException)
                    {
                        ex.ErrorMessage = ex.Exception.Message;
                    }

                    Console.WriteLine(ex.Exception);
                });
            });
        }

        void AddCacheServices()
        {
            services.AddMemoryCache();
            services.AddSingleton<KeyValueCache<ImmutableArray<Entity>>>(p =>
                new KeyValueCache<ImmutableArray<Entity>>(p,
                    p.GetRequiredService<ILogger<KeyValueCache<ImmutableArray<Entity>>>>(),
                    "entities", cmsOptions.EntitySchemaExpiration));

            services.AddSingleton<KeyValueCache<LoadedQuery>>(p =>
                new KeyValueCache<LoadedQuery>(p,
                    p.GetRequiredService<ILogger<KeyValueCache<LoadedQuery>>>(),
                    "query", cmsOptions.QuerySchemaExpiration));
        }

        void AddStorageServices()
        {
            services.AddSingleton(new LocalFileStoreOptions(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"),
                cmsOptions.ImageCompression.MaxWidth,
                cmsOptions.ImageCompression.Quality));
            services.AddSingleton<LocalFileStore>();
        }

       

        void AddDbServices()
        {
            _ = databaseProvider switch
            {
                DatabaseProvider.Sqlite => AddSqliteDbServices(),
                DatabaseProvider.Postgres => AddPostgresDbServices(),
                DatabaseProvider.SqlServer => AddSqlServerDbServices(),
                _ => throw new Exception("unsupported database provider")
            };
            
            services.AddSingleton(new KateQueryExecutorOption(cmsOptions.DatabaseQueryTimeout));
            services.AddSingleton<KateQueryExecutor>();
        }

        IServiceCollection AddSqliteDbServices()
        {
            services.AddSingleton(new KateProviderOption(connectionString));
            services.AddSingleton<IKateProvider,SqliteKateProvider>();
            
            services.AddSingleton(new DefinitionExecutorOptions(connectionString));
            services.AddSingleton<IDefinitionExecutor, SqliteDefinitionExecutor>();
            return services;
        }

        IServiceCollection AddSqlServerDbServices()
        {
            services.AddSingleton(new KateProviderOption(connectionString));
            services.AddSingleton<IKateProvider,SqlServerKateProvider>();
            
            services.AddSingleton(new DefinitionExecutorOptions(connectionString));
            services.AddSingleton<IDefinitionExecutor,SqlServerDefinitionExecutor>();
            return services;
        }

        IServiceCollection AddPostgresDbServices()
        {
            var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
            services.AddSingleton(dataSource);
            services.AddSingleton<IKateProvider,PostgresKateProvider>();
            services.AddSingleton<IDefinitionExecutor,PostgresDefinitionExecutor>();
            return services;
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        PrintVersion();
        await InitSchema();

        UseApiRouters();
        UseGraphql();
        UseExceptionHandler();

        if (cmsOptions.EnableClient)
        {
            UseAdminPanel();
            UserRedirects();
            app.MapStaticAssets();
        }
        return;

        void UserRedirects()
        {
            var options = new RewriteOptions();
            options.AddRedirect(@"^admin$", $"{FluentCmsContentRoot}/admin");
            options.AddRedirect(@"^schema$", $"{FluentCmsContentRoot}/schema-ui/list.html");
            app.UseRewriter(options);
        }

        void UseGraphql()
        {
            app.UseGraphQL<Schema>();
            app.UseGraphQLGraphiQL(cmsOptions.GraphQlPath);
        }

        void UseApiRouters()
        {
            var apiGroup = app.MapGroup(cmsOptions.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/entities").MapEntityHandlers();
            apiGroup.MapGroup("/schemas").MapSchemaHandlers();
            apiGroup.MapGroup("/files").MapFileHandlers();
            apiGroup.MapGroup("/queries").MapQueryHandlers().CacheOutput(cmsOptions.QueryCachePolicy);
            
            // if auth component is not use, the handler will use dummy profile service
            apiGroup.MapGroup("/profile").MapProfileHandlers();
            
            app.MapGroup(cmsOptions.RouteOptions.PageBaseUrl).MapPages().CacheOutput(cmsOptions.PageCachePolicy);
            if (cmsOptions.MapCmsHomePage) app.MapHomePage().CacheOutput(cmsOptions.PageCachePolicy);
        }

        void UseAdminPanel()
        {
            const string adminPanel = "/admin";
            app.MapWhen(context => context.Request.Path.StartsWithSegments($"{FluentCmsContentRoot}{adminPanel}"),
                subApp =>
                {
                    subApp.UseRouting();
                    subApp.UseEndpoints(endpoints =>
                    {
                        endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{adminPanel}",
                            $"{FluentCmsContentRoot}{adminPanel}/index.html");
                        endpoints.MapFallbackToFile($"{FluentCmsContentRoot}{adminPanel}/{{*path:nonfile}}",
                            $"{FluentCmsContentRoot}{adminPanel}/index.html");
                    });
                });
        }

        async Task InitSchema()
        {
            using var serviceScope = app.Services.CreateScope();

            var schemaService = serviceScope.ServiceProvider.GetRequiredService<ISchemaService>();
            await schemaService.EnsureSchemaTable();
            await schemaService.EnsureTopMenuBar();
        }

        void UseExceptionHandler()
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {

                    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (ex is ResultException)
                    {
                        context.Response.StatusCode = 400;
                        var problem = app.Environment.IsDevelopment()
                            ? new Problem(ex.Message, ex.StackTrace)
                            : new Problem(ex.Message);
                        await context.Response.WriteAsJsonAsync(problem);
                    }
                });
            });
        }
    }

    private void PrintVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var parts = dbOptions.ConnectionString.Split(";").Where(x => !x.StartsWith("Password"));

        logger.LogInformation(
            $"""
            *********************************************************
            Using {title}, Version {informationalVersion?.Split("+").First()}
            Database : {dbOptions.Provider} - {string.Join(";", parts)}
            Client App is Enabled :{cmsOptions.EnableClient}
            Use CMS' home page: {cmsOptions.MapCmsHomePage}
            GraphQL Client Path: {cmsOptions.GraphQlPath}
            RouterOption: API Base URL={cmsOptions.RouteOptions.ApiBaseUrl} Page Base URL={cmsOptions.RouteOptions.PageBaseUrl}
            Image Compression: MaxWidth={cmsOptions.ImageCompression.MaxWidth}, Quality={cmsOptions.ImageCompression.Quality}
            Schema Cache Settings: Entity Schema Expiration={cmsOptions.EntitySchemaExpiration}, Query Schema Expiration = {cmsOptions.QuerySchemaExpiration}
            Output Cache Settings: Page CachePolicy={cmsOptions.PageCachePolicy}, Query Cache Policy={cmsOptions.QueryCachePolicy}
            *********************************************************
            """);
    }
}