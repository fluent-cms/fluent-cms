using System.Collections.Immutable;
using System.Reflection;
using FluentCMS.Auth.Handlers;
using FluentCMS.Auth.Services;
using FluentCMS.Cms.Handlers;
using FluentCMS.Cms.Services;
using FluentCMS.Exceptions;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Graph;
using FluentCMS.Options;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.KateQueryExecutor;
using FluentCMS.Utils.LocalFileStore;
using FluentCMS.Utils.PageRender;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Rewrite;
using Npgsql;
using Schema = FluentCMS.Graph.Schema;

namespace FluentCMS.Builders;

public enum DatabaseProvider
{
    Sqlite,
    Postgres,
    SqlServer,
}

public record Problem(string Title, string? Detail =default);

public sealed class CmsBuilder(
    ILogger<CmsBuilder> logger,
    DatabaseProvider databaseProvider,
    string connectionString,
    CmsOptions cmsOptions
)
{
    private const string FluentCmsContentRoot = "/_content/FluentCMS";
    public CmsOptions Options => cmsOptions;

    public static IServiceCollection AddCms(
        IServiceCollection services,
        DatabaseProvider databaseProvider,
        string connectionString,
        Action<CmsOptions>? optionsAction = default)
    {
        var cmsOptions = new CmsOptions();
        optionsAction?.Invoke(cmsOptions);

        services.AddSingleton<CmsBuilder>(p => new CmsBuilder(
            p.GetRequiredService<ILogger<CmsBuilder>>(),
            databaseProvider,
            connectionString,
            cmsOptions
        ));

        InjectDbServices().Ok();
        InjectServices();
        AddGraphql();
        return services;

        void AddGraphql()
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
                    if (ex.Exception is ServiceException)
                    {
                        ex.ErrorMessage = ex.Exception.Message;
                    }

                    Console.WriteLine(ex.Exception);
                });
            });
        }

        void InjectServices()
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

            services.AddSingleton<PageTemplate>(p =>
            {
                var provider = p.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                var fileInfo = provider.GetFileInfo($"{FluentCmsContentRoot}/static-assets/templates/template.html");
                return new PageTemplate(fileInfo.PhysicalPath ?? "");
            });
            services.AddSingleton<LocalFileStore>(_ => new LocalFileStore(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files"), cmsOptions.ImageCompression.MaxWidth,
                cmsOptions.ImageCompression.Quality)
            );
            services.AddSingleton<KateQueryExecutor>(p =>
                new KateQueryExecutor(p.GetRequiredService<IKateProvider>(), cmsOptions.DatabaseQueryTimeout));
            services.AddSingleton<HookRegistry>();

            services.AddScoped<ISchemaService, SchemaService>();
            services.AddScoped<IEntitySchemaService, EntitySchemaService>();
            services.AddScoped<IQuerySchemaService, QuerySchemaService>();

            services.AddScoped<IEntityService, EntityService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPageService, PageService>();

            services.AddScoped<IProfileService, DummyProfileService>();
        }

        Result InjectDbServices()
        {
            return databaseProvider switch
            {
                DatabaseProvider.Sqlite => InjectSqliteDbServices(),
                DatabaseProvider.Postgres => InjectPostgresDbServices(),
                DatabaseProvider.SqlServer => InjectSqlServerDbServices(),
                _ => Result.Fail("unsupported database provider")
            };
        }

        Result InjectSqliteDbServices()
        {
            services.AddSingleton<IKateProvider>(p =>
                new SqliteKateProvider(connectionString, p.GetRequiredService<ILogger<SqliteKateProvider>>()));
            services.AddSingleton<IDefinitionExecutor>(p =>
                new SqliteDefinitionExecutor(connectionString,
                    p.GetRequiredService<ILogger<SqliteDefinitionExecutor>>()));
            return Result.Ok();
        }

        Result InjectSqlServerDbServices()
        {
            services.AddSingleton<IKateProvider>(p =>
                new SqlServerKateProvider(connectionString,
                    p.GetRequiredService<ILogger<SqlServerKateProvider>>()));
            services.AddSingleton<IDefinitionExecutor>(p =>
                new SqlServerDefinitionExecutor(connectionString,
                    p.GetRequiredService<ILogger<SqlServerDefinitionExecutor>>()));
            return Result.Ok();
        }

        Result InjectPostgresDbServices()
        {
            var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
            services.AddSingleton<IKateProvider>((p
            ) =>
            {
                var logger = p.GetRequiredService<ILogger<PostgresKateProvider>>();
                return new PostgresKateProvider(dataSource, logger);
            });
            services.AddSingleton<IDefinitionExecutor>((p
            ) =>
            {
                var logger = p.GetRequiredService<ILogger<PostgresDefinitionExecutor>>();
                return new PostgresDefinitionExecutor(dataSource, logger);
            });
            return Result.Ok();
        }
    }

    public async Task UseCmsAsync(WebApplication app)
    {
        PrintVersion();
        await InitSchema();

        UseApiRouters();
        UseGraphql();
        UseExceptionHandler();

        if (!cmsOptions.EnableClient) return;
        UseAdminPanel();
        UserRedirects();
        app.MapStaticAssets();

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
            await schemaService.EnsureSchemaTable(default);
            await schemaService.EnsureTopMenuBar(default);
        }

        void UseExceptionHandler()
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {

                    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (ex is ServiceException)
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
        var parts = connectionString.Split(";")
            .Where(x => !x.StartsWith("Password"))
            .ToArray();

        logger.LogInformation($"""
                               *********************************************************
                               *********************************************************
                               {title}, Version {informationalVersion?.Split("+").First()}
                               Database : {databaseProvider} - {string.Join(";", parts)}
                               EntitySchemaExpiration: {cmsOptions.EntitySchemaExpiration}
                               QuerySchemaExpiration: {cmsOptions.QuerySchemaExpiration}
                               *********************************************************
                               *********************************************************
                               """);
    }
}