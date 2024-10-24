using FluentCMS.Auth.models;
using FluentCMS.Blog.Data;
using FluentCMS.Services;
using FluentCMS.WebAppExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Blog;


public static class App
{
   const string CorsPolicyName = "AllowAllOrigins";
   public static async Task Run(WebApplicationBuilder builder)
   {
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();
      AddCors(builder);

      var (databaseProvider, connectionString) = GetProviderAndConnectionString(builder);
      AddCms(builder, databaseProvider,connectionString);
      AddDbContext(builder, databaseProvider, connectionString);
      builder.AddCmsAuth<MyUser, IdentityRole, AppDbContext>();

      var app = builder.Build();
      app.UseHttpsRedirection();
      if (app.Environment.IsDevelopment())
      {
         app.UseCors(CorsPolicyName);
         app.UseSwagger();
         app.UseSwaggerUI();
      }
      await Migrate(app);
      await app.UseCmsAsync();
      InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [RoleConstants.Sa]));
      InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [RoleConstants.Admin]));
      app.Run();
   }

   private static async Task Migrate(WebApplication app)
   {
      using var scope = app.Services.CreateScope();
      var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      await ctx.Database.EnsureCreatedAsync();
   }

   private static void AddCms(WebApplicationBuilder builder,string databaseProvider, string connectionString)
   {
      switch (databaseProvider)
      {
         case "Sqlite":
            builder.AddSqliteCms(connectionString);
            break;
         case "Postgres":
            builder.AddPostgresCms(connectionString);
            break;
         case "SqlServer":
            builder.AddSqlServerCms(connectionString);
            break;
         default:
            throw new Exception($"unknown provider {databaseProvider}");
      }
   }

   private static void AddCors(WebApplicationBuilder builder)
   {
      var origins = builder.Configuration.GetValue<string>("AllowedOrigins");
      if (!string.IsNullOrWhiteSpace(origins))
      {
         builder.Services.AddCors(options =>
         {
            options.AddPolicy(CorsPolicyName,
               policy =>
               {
                  policy.WithOrigins(origins.Split(",")).AllowAnyHeader()
                     .AllowCredentials();
               });
         });
      }
   }

   private static void AddDbContext(WebApplicationBuilder builder,string databaseProvider, string connectionString)
   {
      switch (databaseProvider)
      {
         case "Sqlite":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
            break;
         case "Postgres":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            break;
         case "SqlServer":
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            break;
         default:
            throw new Exception($"Not supported Provider {databaseProvider}");
      }
   }

   private static (string, string) GetProviderAndConnectionString(WebApplicationBuilder builder)
   {
      var provider = builder.Configuration.GetValue<string>("DatabaseProvider");
      if (string.IsNullOrWhiteSpace(provider))
      {
         throw new Exception("Not find DatabaseProvider");
      }

      //both key Sqlite and ConnectionString_Sqlite work
      var connection = Environment.GetEnvironmentVariable(provider) ??
                       builder.Configuration.GetConnectionString(provider);
      if (string.IsNullOrWhiteSpace(connection))
      {
         throw new Exception("Not find connection string");
      }  
      return (provider, connection);
   }
}