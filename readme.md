# Fluent CMS
## Why another CMS
- **Performance:** Fluent CMS demonstrates exceptional performance, being 50 times faster than Strapi as detailed in the,
as detailed in the [performance-test-fluent-cms-vs-strapi.md](doc%2Fperformance-test-fluent-cms-vs-strapi.md)
Additionally, it outperforms manually written APIs using ASP.NET/Entity Framework by a factor of 15, 
as detailed in [performance-test-fluent-cms-vs-entity-framework.md](doc%2Fperformance-test-fluent-cms-vs-entity-framework.md)
- **Powerful:**  Leveraging its schema-driven architecture, Fluent CMS performs CRUD operations based on schema definitions 
rather than hard-coded specifics for each entity. This approach reduces repetitive tasks for developers, streamlining the development process.
- **Lightweight:** The codebase of Fluent CMS remains small, clean, and elegant, thanks to the use of modern tools like Entity Framework, SqlKata, PrimeReact, and JasonEditor.

## Play with Fluent CMS
1. Live Demo  
   - Admin Panel https://fluent-cms-admin.azurewebsites.net/
      - Email: `admin@cms.com`
      - Password: `Admin1!`  
   - Public Site : https://fluent-cms-ui.azurewebsites.net/
2. Docker
   1. **Clone the Repository**
      ```shell
      git clone https://github.com/fluent-cms/fluent-cms
      ```
   2. **Bring Up Services**
      ```shell
      cd fluent-cms-sqlite-docker
      docker-compose up
      ```
   3. **Explore the App**
       - Admin Panel: http://localhost:8080, use username `admin@cms.com` and password `Admin1!` to login.
       - Demo Public Site: http://localhost:3000  
3. Source code
   1. **Clone the Repository**
      ```shell
      git clone https://github.com/fluent-cms/fluent-cms
      ```
   2. **Start Admin Panel**
      ```shell
      cd fluent-cms/server/FluentCMS
      dotnet resotore
      dotnet run
      ```
       - Admin Panel: http://localhost:5210, use username `admin@cms.com`, password `Admin1!` to login.   
## Quick Start
For this tutorial, I will walk you though how to build cooking blog website from scratch.
By the end of this tutorials, you have:
1. An admin panel to manage blog content.
2. REST APIs for mobile and web clients.    
3. Detailed in [Quickstart.md](doc%2FQuickstart.md) 
## Development
![overview.png](doc%2Fdiagrams%2Foverview.png)
- Web Server: [FluentCMS](..%2Fserver%2FFluentCMS)
- Admin Panel Client: [admin-ui](..%2Fadmin-ui)
- Schema Builder: [schema-ui](..%2Fserver%2FFluentCMS%2Fwwwroot%2Fschema-ui)
- Demo Publish Site: [ui](..%2Fui)
- detailed in [Development.md](doc%2FDevelopment.md)