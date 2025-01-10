

---
## Integrating it into Your Project

<details>
<summary>
Follow these steps to integrate  FormCMS into your project using a NuGet package.
</summary>

1. **Create a New ASP.NET Core Web Application**.

2. **Add the NuGet Package**:
   To add FormCMS, run the following command:  
   ```
   dotnet add package FormCMS
   ```

3. **Modify `Program.cs`**:
   Add the following line before `builder.Build()` to configure the database connection (use your actual connection string):  
   ```
   builder.AddSqliteCms("Data Source=cms.db");
   var app = builder.Build();
   ```

   Currently,  FormCMS supports `AddSqliteCms`, `AddSqlServerCms`, and `AddPostgresCms`.

4. **Initialize FormCMS**:
   Add this line after `builder.Build()` to initialize the CMS:  
   ```
   await app.UseCmsAsync();
   ```  
   This will bootstrap the router and initialize the  FormCMS schema table.

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

You can find an example project [here](https://github.com/formcms/formcms/tree/main/examples/WebApiExamples).

</details>