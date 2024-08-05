# Fluent CMS - CRUD (Create, Read, Update, Delete) for any entities
[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)
Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms) If you find it useful, please give it a star ⭐
## What is it
Fluent CMS is an open-source content management system designed to streamline web development workflows. It proves valuable even for non-CMS projects by eliminating the need for tedious CRUD API and page development.
- **CRUD APIs:** It offers a set of RESTful CRUD (Create, Read, Update, Delete) APIs for any entities based on your configuration, easily set up using the Schema Builder.
- **Admin Panel UI:** The system includes an Admin Panel UI for data management, featuring a rich set of input types such as datetime, dropdown, image, rich text, and a flexible query builder for data searching.
- **Easy Integration:** Fluent CMS can be seamlessly integrated into your ASP.NET Core project via a NuGet package. You can extend your business logic by registering hook functions that execute before or after database access.
- **Performance:** Fluent CMS is designed with performance in mind, boasting speeds 100 times faster than Strapi (details in the [performance vs Strapi](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-strapi.md) test). It is also 10% faster than manually written APIs using Entity Framework (details in the [performance vs EF](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-entity-framework.md) test).
  
## Live Demo - A blog website based on Fluent CMS 
   source code [FluentCMS.Blog](server%2FFluentCMS.Blog)
   - Admin Panel https://fluent-cms-admin.azurewebsites.net/
      - Email: `admin@cms.com`
      - Password: `Admin1!`  
   - Public Site : https://fluent-cms-ui.azurewebsites.net/
    
## Add Fluent CMS to your own project
1. Create your own WebApplication.
2. Add FluentCMS package
   The next command copies the compiled Admin Panel code to your wwwroot directory. The frontend code, written in React and jQuery, is available in the admin-ui folder within this repository.     
    For Mac, use the following command. For Windows, the directory should be located at $(NuGetPackageRoot)fluentcms\1.0.0\staticwebassets. Please replace `0.0.4` with the correct version number.  
   
   ```shell
   dotnet add package FluentCMS
   cp -a ~/.nuget/packages/fluentcms/0.0.3/staticwebassets wwwroot 
   ```
4. Modify Program.cs, add the following line before builder.Build(), the input parameter is the connection string of database.
   ```
   builder.AddSqliteCms("Data Source=cms.db").PrintVersion();
   var app = builder.Build();
   ```
   Currently FluentCMS support AddSqliteCms, AddSqlServerCms, AddPostgresCMS 

5. Add the following line After builder.Build()
   ```
   await app.UseCmsAsync(false);
   ```
   this function bootstrap router, initialize Fluent CMS schema table

6. If everthing is good, when the app starts, when you go to the home page, you should see the empty Admin Panel
   Here is a quickstart on how to use the Admin Panel [Quickstart.md](https://github.com/fluent-cms/fluent-cms/blob/main/doc/Quickstart.md) 
7. If you want to have a look at how FluentCMS handles one to many, many-to-many relationships, just add the following code
    ```
    var schemaService = app.GetCmsSchemaService();
    await schemaService.AddOrSaveSimpleEntity("student", "Name", null, null);
    await schemaService.AddOrSaveSimpleEntity("teacher", "Name", null, null);
    await schemaService.AddOrSaveSimpleEntity("class", "Name", "teacher", "student");   
   ```
   These code created 3 entity, class and teacher has many-to-one relationship. class and student has many-to-many relationship
8. To Add you own business logic, you can add hook, before or after CRUD. For more hook example, have a look at  [Program.cs](https://github.com/fluent-cms/fluent-cms/blob/main/server%2FFluentCMS.App%2FProgram.cs)
    ```
   var hooks = app.GetCmsHookFactory();
   hooks.AddHook("teacher", Occasion.BeforeInsert, Next.Continue, (IDictionary<string,object> payload) =>
   {
      payload["Name"] = "Teacher " + payload["Name"];
    });
   ```
9. Source code of this example can be found at  [WebApiExamples](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples)  
## Core Concepts
   - Understanding concepts like Entity, Attributes, View is crucial for using and customizing Fluent CMS.     
   - Detailed in [Concepts.md](doc%2FConcepts.md)
## Development
![overview.png](doc%2Fdiagrams%2Foverview.png)
- Web Server: 
  - Code [FluentCMS](..%2Fserver%2FFluentCMS)
  - Doc [Server](doc%2FDevelopment.md#Server )
- Admin Panel Client:
  - Code [admin-ui](..%2Fadmin-ui)
  - Doc [Admin-Panel-UI](doc%2FDevelopment.md#Admin-Panel-UI)
- Schema Builder: 
  - Code [schema-ui](..%2Fserver%2FFluentCMS%2Fwwwroot%2Fschema-ui)
  - Doc [Schema-Builder-UI](doc%2FDevelopment.md#Schema-Builder-UI)
- Demo Publish Site:
  - Code [ui](..%2Fui)
