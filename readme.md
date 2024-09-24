# Fluent CMS - CRUD (Create, Read, Update, Delete) for any entities
[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)
Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms)! If you'd like to contribute to the project, please check out our [CONTRIBUTING guide](https://github.com/fluent-cms/fluent-cms/blob/main/CONTRIBUTING.md). Don’t forget to give us a star ⭐ if you find Fluent CMS helpful!
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
## Live Demo - A blog website based on Fluent CMS
source code [Example Blog Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).
- Admin Panel https://fluent-cms-admin.azurewebsites.net/admin
    - Email: `admin@cms.com`
    - Password: `Admin1!`
- Public Site : https://fluent-cms-admin.azurewebsites.net/

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

## Developing a simple online course system use Fluent CMS
<details>
  <summary>
      The following chapter will guide you through developing a simple online course system, starts with three entity `Teachers`, `Courses`, and `Students`.
  </summary>

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
</details>

## Adding your own business logics 
<details>
  <summary>
      The following chapter will guide you through add your own business logic by add validation logic, hook functions, and produce events to Kafka.
  </summary>

### Add validation logic using simple c# express
You can add simple c# expression to  `Validation Rule` of attributes, the expression is supported by [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso).
for example you can add simple expression like `name != null`.

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

### Produce Events to Kafka
The producing event functionality is implemented by adding hook functions behind the scene,  to enable this functionality, you need add two line of code,
`builder.AddKafkaMessageProducer("localhost:9092");` and `app.RegisterMessageProducerHook()`.

```
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```
</details>

## Permissions Control
<details>
  <summary>FluentCMS authorizes access to each entity by using role-based permissions and custom policies that control user actions like create, read, update, and delete.</summary>

Fluent CMS' permission control module is decoupled from the Content Management module, allowing you to implement your own permission logic or forgo permission control entirely.
The built-in permission control in Fluent CMS offers four privilege types for each entity:
- **ReadWrite**: Full access to read and write.
- **RestrictedReadWrite**: Users can only modify records they have created.
- **Readonly**: View-only access.
- **RestrictedReadonly**: Users can only view records they have created.

Additionally, Fluent CMS supports custom roles, where a user's privileges are a combination of their individual entity privileges and the privileges assigned to their role.

To enable fluentCMS' build-in permission control feature, add the following line to builder.
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
</details>


## **Designing Queries in FluentCMS**
<details>
 <summary>
FluentCMS streamlines frontend development with support for GraphQL-style queries.
</summary>

### Requirements

As shown in the screenshot below, we aim to design a course detail page. In addition to displaying basic course information, the page should also show related entity data, such as:

- Teacher's bio and skills
- Course-related materials, such as videos   
![Course](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-course.png)
### RESTful API

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

</details>

## Design Web Page
<details>
 <summary>
The Page designer is built on the open-source project GrapesJS and Handlebars, allowing you to bind pages with FluentCMS queries for dynamic content rendering.
</summary>

### Introduction to GrapesJS Panels

GrapesJS has a user interface with four main panels. 

![Grapes.js-toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)
1. **Style Manager**: Allows users to customize the CSS properties of selected elements on the canvas, with no modifications made by FluentCMS to this panel.
2. **Traits Panel**: Used to modify attributes of selected elements, with FluentCMS adding custom traits to help the page renderer bind data to pages.
3. **Layers Panel**: Displays a hierarchical view of page elements, similar to the DOM structure. FluentCMS does not customize this panel but it is useful for locating FluentCMS blocks.
4. **Blocks Panel**: Contains pre-made blocks or components for drag-and-drop functionality, with FluentCMS adding its own customized blocks.

### Tailwind CSS support
Fluent CMS page render include the following CSS by default.
```html
    <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/base.min.css">
    <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/components.min.css">
    <link rel="stylesheet" href="https://unpkg.com/@tailwindcss/typography@0.1.2/dist/typography.min.css">
    <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/utilities.min.css">
```
### Page Type: Landing Page vs Detail Page
#### **Landing Page**: A landing page is typically the first page a visitor sees.  
Data in the following example landing page comes from 3 data sources:
   - Featured Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?status=featured
   - Advanced Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Advanced
   - Beginner Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Beginner
![LandingPage](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/landing-page.png)
#### **Detail Page**: A detail page provides in-depth information about a specific item.   
Fluent CMS use the router param to retrieve the specific item

![Course](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/page-course.png)

### Data Binding: single Field or repeat field 


### Image And Link

### Customized Blocks


#### Landing Page
In previous chapter, we have defined query APIs to combine data from multiple entities, now is time to design front-end web pages to render the data.
To manage pages, go to `schema builder` > `pages`.


1. For above page, the data comes from 3 Queries
    - Featured Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?status=featured
    - Advanced Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Advanced
    - Beginner Courses,  https://fluent-cms-admin.azurewebsites.net/api/queries/courses?level=Beginner
2. Drag a Content Block from `Blocks Panel` > `Extra` to Canvas,
   To Bind a multiple records trait to a data source, hover mouse to a element with `Multiple-records` tooltips, select the element, then the traits panels shows. There are the following options
    - field
    - query
    - qs : stands for query string, e.g. the Beginner Course section use level=Beginner to add a constraint only beginner course can show in this section.
    - offset
    - limit
      ![Grapes](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/graps-traits.png)
#### Detail Page
We normally give a router parameter to Detail page, e.g. https://fluent-cms-admin.azurewebsites.net/pages/course/7.  
The suffix `.detail` should be added to page name, the page `course.detail` corresponds to above path.  
Detail page need to call query with query parameter `router.key`

You can also add `Multipe-records` elements to detail page, if you don't specify query, page render tries to resolve the field from query result of the page.
</details>

## Development
<details>
  <summary>The backend is written in ASP.NET Core, the Admin Panel uses React, and the Schema Builder is developed with jQuery</summary>

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
</details>
