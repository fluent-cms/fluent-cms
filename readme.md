# Fluent CMS
## Demo / Quick Start
- Admin Panel https://fluent-cms-admin.azurewebsites.net/
  - Email: `admin@cms.com`
  - Password: `Admin1!`  
- Public Site : https://fluent-cms-ui.azurewebsites.net/

For examples, you are developing a cooking blog and you need:
1. An admin panel to manage blog content.
2. REST APIs for mobile and web clients.

With Fluent CMS, there's no need for coding—just some configuration.

### Add entity
1. Login to Admin Panel
   - Go to Admin Panel https://fluent-cms-admin.azurewebsites.net/,  
   - use Email `admin@cms.com`, Password `Admin1!`   
![img.png](doc/screenshots/admin_panel_login.png) 
2. Navigate to Schema Builder 
   - click the Menu Item  `Schema Builder`    
![img.png](doc/screenshots/admin_panel_home.png)
3. Access the Add Entity Page by 
   - click the Menu Item  `Add Entity`
![img_8.png](doc/screenshots/schema_builder_home.png)
4. Fill in the Entity Detail 
   - On the Add Entity Page
     - Entity name: `blogs`
     - Table name: `blogs`
     - Primary Key: `id`
     - Title Attribute: `title`
     - Default Page Size: `20`
     - Entity Title : `Blogs`                
![img.png](doc/screenshots/schema_builder_entity.png) 
5. Add attributes to the entity
   - The system will add 3 system fields `id`, `created_at`, `updated_at` automatically.
   - Add The following attributes:
       - `title`
       - `published_time`   {Database Type: `Datetime`, Display Type: `dateTime`}
       - `body` {Database Type: `Text`, Display Type: `Editor`}   
   - Click the `Update Database` button to save schema and create a table in database.
![img.png](doc/screenshots/schema_builder_attributes.png)
6. Add Menu item for Admin Panel's Top Menu Bar
   - Click `Schema List`, 
   - Edit the top-menu-bar, add a new item, 
     - Link: `/entity/blogs` 
     - Label: `Blogs`      
![img.png](doc/screenshots/schema_builder_top-menu-bar.png)
7. Manage Content in Admin Panel
   - Click the `Fluent CMS` logo, go to Admin Panel
   - You can see a new menu item `Blogs` is added, 
   - now you can manage content
![img_14.png](doc/screenshots/admin_panel_entity_list.png)
### Add Public API
Normally we don't want expose database data to public user directly, we want 

## System overview
![img.png](doc/overview.png)

## Why another CMS
The typical workflow for web development involves:
The normal workflow for web development is:
1. Backend Developers creating tables and defining relationships in databases.
2. Backend Developers creating APIs to perform CRUD (Create, Read, Update, Delete) operations on data.
3. Frontend Developers creating web pages that call these APIs.

Although ORMs and UI frameworks can reduce some of this work, developers often find themselves repeating the same tasks. 
As projects progress, adding or removing fields in tables necessitates changes to both the frontend and backend code, 
which in turn requires redeploying both applications.

Fluent CMS addresses this issue by not hard coding the backend and frontend to specific entities. 
Instead, they read the schema definition to generate APIs. 
This means that changing an entity attribute only requires updating the schema definition in the schema builder.

### Play With Fluent CMS Using Docker

Assuming you have Docker and Docker Compose installed, follow these steps:

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
    - Manage content at [http://localhost:8080](http://localhost:8080) using username `admin@cms.com` and password `Admin1!`
    - View the demo frontend at [http://localhost:3000](http://localhost:3000)

## Development
### Server
- asp.net core
- entity framework core
- sqlkata, it using dapper ORM behind the scene(https://sqlkata.com/)

Both entity framework and sqlkata can abstract query from specific Database dialect, so I extract database access to
another layer, currently fluent-cms support postgres sql, it can easily support SQL Server and MySQL in the future. 

