[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)  
Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms)! 
If you'd like to contribute to the project, please check out our [CONTRIBUTING guide](https://github.com/fluent-cms/fluent-cms/blob/main/CONTRIBUTING.md).Don’t forget to give us a star  ⭐ if you find Fluent CMS helpful!  

## What is it
Fluent CMS is an open-source Content Management System designed to streamline web development workflows.  
It proves valuable even for non-CMS projects by eliminating the need for tedious CRUD API and page development.  
- **CRUD:** Fluent CMS offers built-in RESTful CRUD (Create, Read, Update, Delete) APIs along with an Admin Panel that supports a wide range of input types, including datetime, dropdown, image, and rich text, all configurable to suit your needs.  
- **GraphQL-style Query** Retrieve multiple related entities in a single call, enhancing security, performance, and flexibility on the client side.  
- **Wysiwyg Web Page Designer:** Leveraging [Grapes.js](https://grapesjs.com/) and [HandleBars](https://handlebarsjs.com/), the page designer allows you to create pages and bind query data without coding.  
- **Permission Control** Assign read/write, read-only, access to entities based on user roles or individual permissions.  
- **Integration and extension** Fluent CMS can be integrated into projects via a NuGet package.  
  Validation logic can be implemented using C# statements through [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso),
  and complex functionalities can be extended using CRUD Hook Functions.
  Additionally, Fluent CMS supports message brokers like Kafka for CRUD operations.  
- **Performance:** Utilizing [SqlKata](https://sqlkata.com/) and [Dapper](https://www.learndapper.com/), Fluent CMS achieves performance levels comparable to manually written RESTful APIs using Entity Framework Core. Performance benchmarks include comparisons against Strapi and Entity Framework.  
    - [performance vs Strapi](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-strapi.md)  
    - [performance vs EF](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-entity-framework.md)  
## Live Demo - A online course website based on Fluent CMS
source code [Example Blog Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).  
- Admin Panel https://fluent-cms-admin.azurewebsites.net/admin  
    - Email: `admin@cms.com`  
    - Password: `Admin1!`  
- Public Site : https://fluent-cms-admin.azurewebsites.net/  


## Adding Fluent CMS to your own project
The following chapter will guid you through add Fluent CMS to your own project by adding a nuget package. 

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
   You can also copy these two app manually, you can find these two app at nuget's package folder.  
   By default, when you install a NuGet package, it gets stored in a global cache folder on your machine. You can find it at:
   - Windows: C:\Users\<YourUsername>\.nuget\packages
   - Mac/Linux: ~/.nuget/packages
   You can find fluentCMS client Apps at `<NuGet package directory>\fluentcms\<version>\staticwebassets`

Now that the web server is up and running, the next chapter will guide you through building the schema and managing data.
The example project can be found at [Example Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).


## Developing a simple online course system use Fluent CMS
The following chapter will guide you through developing a simple online course system, starts with three entity `Teachers`, `Courses`, and `Students`. 

### Database Schema
#### 1. **Teachers Table**
This table stores information about the teachers.

| Column Name   | Data Type  | Description                 |
|---------------|------------|-----------------------------|
| `Id`          | `INT`      | Primary Key, unique ID for each teacher. |
| `FirstName`   | `VARCHAR`  | Teacher's first name.        |
| `LastName`    | `VARCHAR`  | Teacher's last name.         |
| `Email`       | `VARCHAR`  | Teacher's email address.     |
| `PhoneNumber` | `VARCHAR`  | Teacher's contact number.    |

#### 2. **Courses Table**
This table stores information about the courses.

| Column Name   | Data Type  | Description                   |
|---------------|------------|-------------------------------|
| `Id`          | `INT`      | Primary Key, unique ID for each course. |
| `CourseName`  | `VARCHAR`  | Name of the course.            |
| `Description` | `TEXT`     | Brief description of the course. |
| `TeacherId`   | `INT`      | Foreign Key, references `TeacherId` in the `Teachers` table. |

#### 3. **Students Table**
This table stores information about the students.

| Column Name      | Data Type  | Description                   |
|------------------|------------|-------------------------------|
| `Id`             | `INT`      | Primary Key, unique ID for each student. |
| `FirstName`      | `VARCHAR`  | Student's first name.         |
| `LastName`       | `VARCHAR`  | Student's last name.          |
| `Email`          | `VARCHAR`  | Student's email address.      |
| `EnrollmentDate` | `DATE`     | Date when the student enrolled. |

#### 4. **Enrollments Table (Junction Table)**
This table manages the many-to-many relationship between `Students` and `Courses`, since a student can enroll in multiple courses, and a course can have multiple students.

| Column Name   | Data Type  | Description                           |
|---------------|------------|---------------------------------------|
| `EnrollmentId`| `INT`      | Primary Key, unique ID for each enrollment. |
| `StudentId`   | `INT`      | Foreign Key, references `StudentId` in the `Students` table. |
| `CourseId`    | `INT`      | Foreign Key, references `CourseId` in the `Courses` table. |

#### Relationships:
- **Teachers to Courses**: One-to-Many (A teacher can teach multiple courses, but a course is taught by only one teacher).
- **Students to Courses**: Many-to-Many (A student can enroll in multiple courses, and each course can have multiple students).
### Build Schema use Fluent CMS Schema builder
After starting your ASP.NET Core application, you will find a menu item labeled "Schema Builder" on the application's home page.

In the Schema Builder, you can add entities such as "Teacher" and "Student."

When adding the "Course" entity, start by adding basic attributes like "Name" and "Description." You can then define relationships by adding attributes as follows:

1. **Teacher Attribute:**  
   Configure it with the following settings:
   ```json
   {
      "DataType": "Int",
      "Field": "teacher",
      "Header": "Teacher",
      "InList": true,
      "InDetail": true,
      "IsDefault": false,
      "Type": "lookup",
      "Options": "teacher"
   }
   ```

2. **Students Attribute:**  
   Configure it with these settings:
   ```json
   {
      "DataType": "Na",
      "Field": "students",
      "Header": "Students",
      "InList": false,
      "InDetail": true,
      "IsDefault": false,
      "Type": "crosstable",
      "Options": "student"
   }
   ```

With these configurations, your minimal viable product is ready to use.

## Adding your own business logics 
  <summary> The following chapter will guide you through add your own business logic by add validation logic, hook functions, and produce events to Kafka. </summary>

### Add validation logic using simple c# express

#### Simple C# logic
You can add simple c# expression to  `Validation Rule` of attributes, the expression is supported by [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso).  
For example, you can add simple expression like `name != null`.  
You can also add `Validation Error Message`, the end user can see this message if validate fail.

#### Regular Expression Support
`Dynamic Expresso` supports regex, for example you can write Validation Rule `Regex.IsMatch(email, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")`.   
Because `Dyamic Expresso` doesn't support [Verbatim String](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim), you have to escape `\`.

### Extent functionality by add Hook functions
You need to add your own Business logic, for examples, you want to verify if the email and phone number of entity `teacher` is valid.
you can register a cook function before insert or update teacher
```
app.RegisterCmsHook("teacher", [Occasion.BeforeInsert, Occasion.BeforeUpdate],(IDictionary<string,object> teacher) =>
{
    var (email, phoneNumber) = ((string)teacher["email"], (string)teacher["phone_number"]);
    if (!IsValidEmail())
    {
        throw new InvalidParamException($"email `{email}` is invalid");
    }
    if (!IsValidPhoneNumber())
    {
        throw new InvalidParamException($"phone number `{phoneNumber}` is invalid");
    }
}
```

### Produce Events to Event Broker(e.g.Kafka)
You can also choose produce events to Event Broker(e.g.Kafka), so Consumer Application function can implement business logic in a async manner.
The producing event functionality is implemented by adding hook functions behind the scene,  to enable this functionality, you need add two line of code,
`builder.AddKafkaMessageProducer("localhost:9092");` and `app.RegisterMessageProducerHook()`.

```
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```


## Permissions Control

Fluent CMS' permission control module is decoupled from the Content Management module, allowing you to implement your own permission logic or forgo permission control entirely.
The built-in permission control in Fluent CMS offers four privilege types for each entity:
- **ReadWrite**: Full access to read and write.
- **RestrictedReadWrite**: Users can only modify records they have created.
- **Readonly**: View-only access.
- **RestrictedReadonly**: Users can only view records they have created.

Additionally, Fluent CMS supports custom roles, where a user's privileges are a combination of their individual entity privileges and the privileges assigned to their role.

To enable fluentCMS' build-in permission control feature, add the following line .
```
//add fluent cms' permission control service 
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();
```
And add the follow line after app was built
```
//user fluent permission control feature
app.UseCmsAuth<IdentityUser>();
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]));
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]));
```
Behind the scene, fluentCMS leverage the hook mechanism.


## **Designing Queries in FluentCMS**

### Requirements

As shown in the screenshot below, we aim to design a course detail page. In addition to displaying basic course information, the page should also show related entity data, such as:

- Teacher's bio and skills
- Course-related materials, such as videos   
![Course](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-course.png)
### RESTFul API

FluentCMS provides Query APIs that address the following needs, similar to GraphQL:

- **Single API Call**: Retrieve all related data with one API call.
- **Sensitive Information Protection**: Safeguard sensitive details, like a teacher's phone number, from being exposed.
- **Performance**: Optimize performance by reducing resource-intensive database queries for public access.

To create or edit a query, navigate to **Schema Builder > Queries**.

### Query Structure

A query is composed of three key parts:

#### 1. Selection Set

The primary entity in the examples below is `course`:

- `teacher` is a lookup attribute of the course.
- `skills` is a cross-table attribute of `teacher`.
- `materials` is a cross-table attribute of `course`.

```graphql
{
    id,
    name,
    desc,
    image,
    level,
    status,
    teacher{
        firstname,
        lastname,
        image,
        bio,
        skills{
            name,
            years
        }
    },
    materials{
        name,
        image,
        link
    }
}
```

#### 2. Sorts

FluentCMS employs **cursor-based pagination**, which is more stable for large datasets compared to offset-based pagination. Cursor-based pagination fetches the next page based on the last cursor. Sorting is handled as follows:

```json
{
  "sorts": [
    {
      "fieldName": "id",
      "order": "Desc"
    }
  ]
}
```

#### 3. Filter

To avoid resource-intensive queries, restrict the number of parameters that can be exposed. In the example below, `qs.id` resolves the ID from the query string parameter `id`. The prefix `qs.` indicates that the value should be fetched from the query string.

Example API call: `/api/queries/<query-name>/one?id=3`

SQL equivalent: `SELECT * FROM courses WHERE level = 'advanced' AND id = 3`

```json
{
  "filters": [
    {
      "fieldName": "level",
      "operator": "and",
      "omitFail": false,
      "constraints": [
        {
          "match": "in",
          "value": "advanced"
        }
      ]
    },
    {
      "fieldName": "id",
      "operator": "and",
      "omitFail": true,
      "constraints": [
        {
          "match": "in",
          "value": "qs.id"
        }
      ]
    }
  ]
}
```

### Query Endpoints

Each query has three corresponding endpoints:

- **List**: `/api/queries/<query-name>` retrieves a paginated list.
  - To view the next page: `/api/queries/<query-name>?last=***`
  - To view the previous page: `/api/queries/<query-name>?first=***`

Example response:

```json
{
  "items": [],
  "first": "",
  "hasPrevious": false,
  "last": "eyJpZCI6M30",
  "hasNext": true
}
```

- **Single Record**: `/api/queries/<query-name>/one` returns the first record.
  - Example: `/api/queries/<query-name>/one?id=***`

- **Multiple Records**: `/api/queries/<query-name>/many` returns multiple records.
  - Example: `/api/queries/<query-name>/many?id=1&id=2&id=3`

If the number of IDs exceeds the page size, only the first set will be returned.

### Cache Settings

- **Query Settings**: Cached in memory for 1 minute.
- **Query Results**: Not cached. A standalone cache module is planned for future implementation.




## Designing Web Page in FluentCMS


### Introduction to GrapesJS Panels
The GrapesJS Page Designer UI provides a toolbox with four main panels:  
![GrapesJS Toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)  
1. **Style Manager**: Lets users customize CSS properties of selected elements on the canvas. FluentCMS does not modify this panel.  
2. **Traits Panel**: Allows you to modify attributes of selected elements. FluentCMS adds custom traits to bind data to components here.  
3. **Layers Panel**: Displays a hierarchical view of page elements similar to the DOM structure. FluentCMS does not customize this panel, but it’s useful for locating FluentCMS blocks.  
4. **Blocks Panel**: Contains pre-made blocks or components for drag-and-drop functionality. FluentCMS adds its own customized blocks here.  

### Tailwind CSS Support
FluentCMS includes Tailwind CSS by default for page rendering, using the following styles:

```html
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/base.min.css">
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/components.min.css">
<link rel="stylesheet" href="https://unpkg.com/@tailwindcss/typography@0.1.2/dist/typography.min.css">
<link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/utilities.min.css">
```

### Page Types: Landing Page, Detail Page, and Home Page

#### **Landing Page**: A landing page is typically the first page a visitor sees.   
The URL format is `/page/<pagename>`.    
A landing page is typically composed of multiple `Multiple Records Components`, each with its own `Query`, making the page-level `Query` optional.

![Landing Page](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-landing.png)

#### **Detail Page**: A detail page provides specific information about an item.  
The URL format is `/page/<pagename>/<router parameter>`, FluentCMS retrieves data by passing the router parameter to the `FluentCMS Query`. 

For the following settings  
- Page Name: `course/{id}`  
- Query: `courses`  
FluentCMS will call the query `https://fluent-cms-admin.azurewebsites.net/api/queries/courses/one?id=3` for URL `https://fluent-cms-admin.azurewebsites.net/pages/course/3`

![Course Detail Page](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-course.png)

#### **Home Page**:
The homepage is a special landing page with the name `home`. Its URL is `/pages/home`. If no other route handles the path `/`, FluentCMS will render `/` as `/pages/home`.

### Data Binding: Singleton or Multiple Records

FluentCMS uses [Handlebars expression](https://github.com/Handlebars-Net/Handlebars.Net) for dynamic data binding.

#### Singleton
Singleton fields are enclosed within `{{ }}`.

![Singleton Field](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-single-field.png)

#### Multiple Records
`Handlebars` loops over arrays using the `each` block.

```handlebars
{{#each course}}
    <li>{{title}}</li>
{{/each}}
```

However, you won’t see the `{{#each}}` statement in the GrapesJS Page Designer. FluentCMS adds it automatically for any block under the `Multiple Records` category.

Steps to bind multiple records:  
1. Drag a block from the `Multiple Records` category.
    ![Multiple Record Blocks](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-block.png)
2. Hover over the GrapesJS components to find a block with the `Multiple-records` tag in the top-left corner, then click the `Traits` panel. You can also use the GrapesJS Layers Panel to locate the component.
    ![Multiple Record Select](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-select.png)  
3. In the `Traits` panel, you have the following options:
    - **Field**: Specify the field name for the Page-Level Query (e.g., for the FluentCMS Query below, you could set the field as `teacher.skills`).  
      ```json
      {
        "teacher": {
          "firstname": "",
          "skills": [
            {
              "name": "cooking fish",
              "years": 3
            }
          ]
        }
      }
      ```   
    - **Query**: The query to retrieve data.  
    - **Qs**: Query string parameters to pass (e.g., `?status=featured`, `?level=Advanced`).  
    - **Offset**: Number of records to skip.  
    - **Limit**: Number of records to retrieve.  
   ![Multiple Record Trait](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-multiple-record-trait.png)

### Linking and Images

FluentCMS does not customize GrapesJS' Image and Link components, but locating where to input `Query Field` can be challenging. The steps below explain how to bind them.

- **Link**:
  Locate the link by hovering over the GrapesJS component or finding it in the `GrapesJS Layers Panel`. Then switch to the `Traits Panel` and input the detail page link, e.g., `/pages/course/{{id}}`. FluentCMS will render this as `<a href="/pages/course/3">...</a>`.

- **Image**:
  Double-click on the image component, input the image path, and select the image. For example, if the image field is `thumbnail_image_url`, input `/files/{{thumbnail_image_url}}`. FluentCMS will replace `{{thumbnail_image_url}}` with the actual field value.

  ![Designer Image](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-image.png)

### Customized Blocks
FluentCMS adds customized blocks to simplify web page design and data binding for `FluentCMS Queries`. These blocks use Tailwind CSS.

- **Multiple Records**: Components in this category contain subcomponents with a `Multiple-Records` trait.
- **Card**: Typically used in detail pages.
- **Header**: Represents a navigation bar or page header.


## Development Guide

### System Overviews
![System Overview](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/diagrams/overview.png)   
- [**Backend Server**](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS)  
- [**Admin Panel UI**](https://github.com/fluent-cms/fluent-cms/tree/main/admin-panel)  
- [**Schema Builder**](https://github.com/fluent-cms/fluent-cms/tree/main/schema-builder)  

### Backend Server
- **Tools**:  
    - **ASP.NET Core**  
    - **SqlKata**: [SqlKata](https://sqlkata.com/)  

![API Controller Service](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/diagrams/api-controller-service.png)

### Admin Panel UI
- **Tools**:
    - **React**
    - **PrimeReact**: [PrimeReact UI Library](https://primereact.org/)
    - **SWR**: [Data Fetching/State Management](https://swr.vercel.app/)

![Admin Panel Sequence](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/diagrams/admin-panel-sequence.png)

### Schema Builder UI
- **Tools**:
    - **jsoneditor**: [JSON Editor](https://github.com/json-editor/json-editor)

![Schema Builder Sequence](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/diagrams/schema-builder-sequence.png)
