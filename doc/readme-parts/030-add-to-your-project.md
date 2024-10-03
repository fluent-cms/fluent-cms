

## Adding Fluent CMS to your own project
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

When the web server is up and running,  you can access Admin Panel by url `/admin`, you can access Schema builder by url `/schema`.
The example project can be found at [Example Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).
</details>