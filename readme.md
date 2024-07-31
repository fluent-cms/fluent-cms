# Fluent CMS

[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)

Welcome to Fluent CMS! If you find it useful, please give it a star ⭐


## Why another CMS
- **Performance:** Fluent CMS demonstrates exceptional performance, being 100 times faster than Strapi as detailed in the
[performance-test-fluent-cms-vs-strapi.md](doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-strapi.md).
Additionally, the performance-critical APIs(use SQLKata/Dapper instead of Entity Framework) are 10% faster than manually written APIs using ASP.NET/Entity Framework, 
as detailed in [performance-test-fluent-cms-vs-entity-framework.md](doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-entity-framework.md)
- **Powerful:**  Leveraging its schema-driven architecture, Fluent CMS performs CRUD operations based on schema definitions 
rather than hard-coded specifics for each entity. This approach reduces repetitive tasks for developers, streamlining the development process.
- **Superior ASP.NET Integration:** Fluent CMS provides a generic CRUD framework, allowing you to easily apply your business logic by adding hooks. 

## Play with Fluent CMS
1. Live Demo  
   - Admin Panel https://fluent-cms-admin.azurewebsites.net/
      - Email: `admin@cms.com`
      - Password: `Admin1!`  
   - Public Site : https://fluent-cms-ui.azurewebsites.net/
2. Source code
      ```shell
      git clone https://github.com/fluent-cms/fluent-cms #clone the repository
      cd fluent-cms/server/FluentCMS
      dotnet restore
      dotnet run  # then you can browse admin panel http://localhost:5210, use username `admin@cms.com`, password `Admin1!` to login.   
      ```
## Quick Start
1. Create your own WebApplication.
2. Add FluentCMS package
   ```shell
   dotnet add package FluentCMS
   # the next command copy compiled Admin Panel code to your wwwroot, 
   # The frontend code was write in React and Jquery, source code is admin-ui, also in this repo
   # The following command is for Mac, for windows the directory should be at $(NuGetPackageRoot)fluentcms\1.0.0\staticwebassets
   # Please change 0.0.3 to the correct version number    
   cp -a ~/.nuget/packages/fluentcms/0.0.3/staticwebassets wwwroot 
   ```
3. Modify Program.cs, add the following line before builder.Build(), the input parameter is the connection string of database.
   ```
   //Currently FluentCMS support AddSqliteCms, AddSqlServerCms 
   builder.AddSqliteCms("Data Source=cms.db").PrintVersion();
   var app = builder.Build();
   ```
4. Add the following line After builder.Build()
   ```
   //this function bootstrap router, initialize Fluent CMS schema table
   await app.UseCmsAsync(false);
   ```
5. If everthing is good, when the app starts, when you go to the home page, you should see the empty Admin Panel
   Here is a quickstart on how to use the Admin Panel [Quickstart.md](doc%2FQuickstart.md) 
6. If you want to have a look at how FluentCMS handles one to many, many-to-many relationships, just add the following code
    ```
    var schemaService = app.GetCmsSchemaService();
    await schemaService.AddOrSaveSimpleEntity("student", "Name", null, null);
    await schemaService.AddOrSaveSimpleEntity("teacher", "Name", null, null);
    await schemaService.AddOrSaveSimpleEntity("class", "Name", "teacher", "student");   
   ```
   These code created 3 entity, class and teacher has many-to-one relationship. class and student has many-to-many relationship
7. To Add you own business logic, you can add hook, before or after CRUD. For more hook example, have a look at  [Program.cs](server%2FFluentCMS.App%2FProgram.cs)
    ```
   var hooks = app.GetCmsHookFactory();
   hooks.AddHook("teacher", Occasion.BeforeInsert, Next.Continue, (IDictionary<string,object> payload) =>
   {
      payload["Name"] = "Teacher " + payload["Name"];
    });
   ```
8. Source code of this example can be found at  [WebApiExamples](examples%2FWebApiExamples)  
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
## Deployment
Fluent CMS has terraForm code for deploy it to AWS EKS, view the wiki page for detail https://github.com/fluent-cms/fluent-cms/wiki/Deploy-Asp.net-Core-Application(Fluent%E2%80%90CMS)-to-Cloud-(EKS-%E2%80%90-AWS-Elastic-Kubernetes-Service)--using-terraform-and-helm
