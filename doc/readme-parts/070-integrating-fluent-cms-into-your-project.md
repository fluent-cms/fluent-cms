

---
## Integrating it into Your Project

<details>
<summary>
Follow these steps to integrate Fluent CMS into your project using a NuGet package.
</summary>

---

1. **Create a New ASP.NET Core Web Application**.

2. **Add the FluentCMS NuGet Package**:
   To add Fluent CMS, run the following command:  
   ```
   dotnet add package FluentCMS
   ```

3. **Modify `Program.cs`**:
   Add the following line before `builder.Build()` to configure the database connection (use your actual connection string):  
   ```
   builder.AddSqliteCms("Data Source=cms.db");
   var app = builder.Build();
   ```

   Currently, Fluent CMS supports `AddSqliteCms`, `AddSqlServerCms`, and `AddPostgresCms`.

4. **Initialize Fluent CMS**:
   Add this line after `builder.Build()` to initialize the CMS:  
   ```
   await app.UseCmsAsync();
   ```  
   This will bootstrap the router and initialize the Fluent CMS schema table.

5. **Optional: Set Up User Authorization**:
   If you wish to manage user authorization, you can add the following code. If you're handling authorization yourself or don’t need it, you can skip this step.  
   ```
   builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
   builder.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();
   ```

   If you'd like to create a default user, add this after `app.Build()`:
   ```
   InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]));
   ```

Once your web server is running, you can access the **Admin Panel** at `/admin` and the **Schema Builder** at `/schema`.

You can find an example project [here](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).

---

</details>