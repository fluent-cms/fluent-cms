# Fluent CMS - CRUD (Create, Read, Update, Delete) for any entities
[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)
Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms) If you find it useful, please give it a star ⭐
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
The example project can be found at [Example Blog Project](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).
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
   Now that the web server is up and running, the next chapter will guide you through building the schema and managing data.

## Develop a simple educational system use Fluent CMS
When designing a database schema for a simple educational system, you typically need to create tables for `Teachers`, `Courses`, and `Students`.
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

In the Schema Builder UI, you can add entities such as "Teacher" and "Student."

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

## Extent functionality by add Hook functions
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
## Permissions Control

Fluent CMS's permission control module is decoupled from the Content Management module, allowing you to implement your own permission logic or forgo permission control entirely.

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
## Design Query
Here’s a text-based layout representation of the web page of the course introduction page.

---
**Introduction to Web Development**  
**Description:**
This course provides an overview of web development...  
**Teacher: John Doe**

- **Skills:**
    - C++ (3 years)
    - C# (5 years)
    - HTML (7 years)
    - Database (4 years)

**Materials:**
- [Week 1: Introduction to HTML](file:///2024-08/75dd9a00.txt)
- [HTML Basics](https://www.youtube.com/watch?v=salY_Sm6mv4&pp=ygULaHRtbCBiYXNpY3M%3D)
---
The data comes from several entities,
- course
- teacher
- skills
- teacher_skill
- material
- course_material

Fluent CMS offers `Query` APIs to meet the following needs, similar to GraphQL queries:

1. **Single API Call:** Allows the frontend to retrieve all related data with just one API call.
2. **Protection of Sensitive Information:** Prevents sensitive data, like the teacher's phone number, from being exposed to the frontend.
3. **Performance:** Reduces resource-intensive database queries, making it more suitable for public access.

To create or edit a query, navigate to `Schema Builder` > `Queries`.

A query has 3 parts
### Selection Set
In the examples below, the main entity is `course`:
- `teacher` is a `lookup` attribute of `course`.
- `skills` is a `crosstable` attributes of `teacher`.`
- `materials` is a `crosstable` attributes of `course`.
```
{
    name, 
    id, 
    description,
    teacher{
        firstname, 
        lastname, 
        skills{
            name, 
            years
        }
    },
    materials{
        name,
        link, 
        file
    }
}
```
### Sorts
FluentCMS uses cursor-based pagination, unlike GraphQL, which supports both cursor- and offset-based pagination.
Offset-based pagination is less stable and unsuitable for large datasets.

Cursor-based pagination retrieves the next page based on the last cursor. FluentCMS calculates the cursor and sorts data as shown below:

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
### Filter
To prevent resource-intensive queries from the frontend, limit the number of exposed parameters.
In the filter definition below, `qs.id` tries to resolve the ID from the query string parameter `id`.
The `qs.` prefix indicates that the value should be fetched from the query string, with the part after `qs.` representing the key of the query string parameter.

For example, the API call /api/queries/<query-name>/one?id=3 corresponds to the SQL query:
`select * from courses where level='advanced' and id=3`
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
Each query definition corresponds to three endpoints:

####  List: `/api/queries/<query-name>` - retrieves a paginated list
- To view next page: `/api/queries/<query-name>?last=***`
- To view previous page: `/api/queries/<query-name>?first=***`

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
#### Single Record:  /api/queries/<query-name>/one - Returns the first record
Example: `/api/queries/<query-name>/one?id=***`

#### Multiple Record:  /api/queries/<query-name>/many
- Returns multiple records based on specified values.
  Example: `/api/queries/<query-name>/one?id=1&id=2&id=3`.

If the number of IDs exceeds the allowed page size, only the first set of records will be returned.
### Cache Settings:
- Query Settings are cached in memory for 1 minutes.
- Query Result are not cached because caching large data to memory is tricky and I intend implement stand alone cache module.

## Design Web Page


## Produce Events to Kafka
The producing event functionality is implemented by adding hook functions behind the scene,  to enable this functionality, you need add two line of code,
`builder.AddKafkaMessageProducer("localhost:9092");` and `app.RegisterMessageProducerHook()`.

```
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```
## We welcome contributions!
If you're interested in improving FluentCMS, please read our [CONTRIBUTING.md](https://github.com/fluent-cms/fluent-cms/blob/main/CONTRIBUTING.md) guide.

## Development
### System Overviews
![System Overview](https://raw.githubusercontent.com/fluent-cms/fluent-cms/main/doc/diagrams/overview.png)
- [**Backend Server**](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS)
- [**Admin Panel UI**](https://github.com/fluent-cms/fluent-cms/tree/main/admin-panel)
- [**Schema Builder**](https://github.com/fluent-cms/fluent-cms/tree/main/schema-ui)
- [**Demo Next.js Public Site**](https://github.com/fluent-cms/fluent-cms/tree/main/examples/BlogPublicSiteNextJsUI)

### Web Server
- **Tools**:
    - **ASP.NET Core**
    - **SqlKata**: [SqlKata](https://sqlkata.com/)

![API Controller Service](https://raw.githubusercontent.com/fluent-cms/fluent-cms/main/doc/diagrams/api-controller-service.png)

### Admin Panel UI
- **Tools**:
    - **React**
    - **PrimeReact**: [PrimeReact UI Library](https://primereact.org/)
    - **SWR**: [Data Fetching/State Management](https://swr.vercel.app/)

![Admin Panel Sequence](https://raw.githubusercontent.com/fluent-cms/fluent-cms/main/doc/diagrams/admin-panel-sequence.png)

### Schema Builder UI
- **Tools**:
    - **jsoneditor**: [JSON Editor](https://github.com/json-editor/json-editor)

![Schema Builder Sequence](https://raw.githubusercontent.com/fluent-cms/fluent-cms/main/doc/diagrams/schema-builder-sequence.png)