# Quick Start
For this tutorial, I will walk you though how to build cooking blog website backend APIs from scratch.
By the end of this tutorials, you have:
1. CRUD pages for manage content in Admin Panel.
2. REST APIs for mobile and web clients.

With Fluent CMS, there's no need for coding—just some configuration.

## Build Schema
![quickstart-schema-builder.png](diagrams%2Fquickstart-schema-builder.png)
### Add entity
1. Login to Admin Panel
    - Go to Admin Panel https://fluent-cms-admin.azurewebsites.net/ or http://localhost:5210 ,
    - use Email `admin@cms.com`, Password `Admin1!`         
      ![img.png](screenshots/admin_panel_login.png)
2. Navigate to Schema Builder
    - click the Menu Item  `Schema Builder`         
      ![img.png](screenshots/admin_panel_home.png)
3. Access the Add Entity Page by
    - click the Menu Item  `Add Entity`      
      ![img_8.png](screenshots/schema_builder_home.png)
4. Fill in the Entity Detail
    - On the Add Entity Page
        - Entity name: `blog`
        - Table name: `blogs`
        - Primary Key: `id`
        - Title Attribute: `title`
        - Default Page Size: `20`
        - Entity Title : `Blogs`                
      ![img_2.png](screenshots/img_2.png)
### Add Fields/Attributes
1. Add attributes to the entity
    - The system will add 3 system fields `id`, `created_at`, `updated_at` automatically.
    - Add The following attributes:
        - `title`
        - `published_time`   {Database Type: `Datetime`, Display Type: `dateTime`}
        - `body` {Database Type: `Text`, Display Type: `Editor`}
    - Click the `Update Database` button to save schema and create a table in database.  
   ![img_3.png](screenshots/img_3.png)
### Add Entity Relationships
   - 
![img_4.png](screenshots/img_4.png)

### Add Public API
Fluent CMS provides a set of CRUD Rest APIs for Admin Panel, these APIs are protected by Authentication.
We don't want expose these APIs to public user directly, We want expose API to public by carefully selected
- Attributes to expose
- Order
- Filter
- Page Size of each request

Follow the follow steps to define a public API
1. Fill the View Detail
    - Navigate to Schema Builder
    - Click the Menu Item `Add View`
    - Fill the View Detail
        - View Name : `lastest-blog`
        - Entity Name : `blogs`
        - Page Size : 10
          ![img.png](screenshots/schema_builder_view_detail.png)
2. Fill Sort Detail
    - Click `+ row` button
    - Input the Sort Detail
        - Attribute Name : `published_at`
        - Order Direction: `Desc`
    - Click `Save Schema` button
      ![img_2.png](screenshots/schema_builder_view_sorts.png)
3. Test the public API
- Access https://fluent-cms-admin.azurewebsites.net/api/views/latest-blogs from browser
  ![img_1.png](screenshots/public_api.png)


## Manage Content in Admin Panel
![quickstart-admin-panel.png](diagrams%2Fquickstart-admin-panel.png)


- Click the `Fluent CMS` logo, go to Admin Panel
- You can see a new menu item `Blogs` is added,
- now you can manage content   
![img_14.png](screenshots/admin_panel_entity_list.png "Entity List Page")
