

## Add Fluent CMS to your own project
<details>
  <summary>
      The following chapter will guid you through add Fluent CMS to your own project by adding a nuget package.
  </summary>

1. Create your own Asp.net Core WebApplication.
2. Add FluentCMS package
   ```shell
   dotnet add package FluentCMS
   ```
3. Modify Program.cs, add the following line before builder.Build(), the input parameter is the connection string of database.
   ```
   builder.AddSqliteCms("Data Source=cms.db");
   var app = builder.Build();
   ```
   Currently FluentCMS support `AddSqliteCms`, `AddSqlServerCms`, `AddPostgresCMS`.

4. Add the following line After builder.Build()
   ```
   await app.UseCmsAsync();
   ```
   this function bootstrap router, initialize Fluent CMS schema table
5. Copy client file to wwwroot of your web project, fluentCMS have two client app `admin` and `schema-ui`, the folder structure looks like below.
   ```
   wwwroot
   --schema-ui
   --admin
   --favicon.ico
   ```
   When you start your web app for the first time, in function `app.UseCmsAsync`, if fluentCMS didn't find client app, it will try to copy client file to `wwwroot` you application. 
   After copy these files, it will prompt `FluentCMS client files are copied to wwwroot, please start the app again`.
   You can also copy these two app manually, you can find these two app at nuget's package folder
   By default, when you install a NuGet package, it gets stored in a global cache folder on your machine. You can find it at:
   - Windows: C:\Users\<YourUsername>\.nuget\packages
   - Mac/Linux: ~/.nuget/packages
   You can find fluentCMS client Apps at `<NuGet package directory>\fluentcms\<version>\staticwebassets`

Now that the web server is up and running, the next chapter will guide you through building the schema and managing data.
The example project can be found at [Example Blog Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).
</details>