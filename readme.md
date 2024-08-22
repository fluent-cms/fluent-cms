# Fluent CMS - CRUD (Create, Read, Update, Delete) for any entities
[![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)
Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms) If you find it useful, please give it a star ⭐
## What is it
Fluent CMS is an open-source content management system designed to streamline web development workflows. It proves valuable even for non-CMS projects by eliminating the need for tedious CRUD API and page development.
- **CRUD APIs:** It offers a set of RESTful CRUD (Create, Read, Update, Delete) APIs for any entities based on your configuration, easily set up using the Schema Builder.
- **Admin Panel UI:** The system includes an Admin Panel UI for data management, featuring a rich set of input types such as datetime, dropdown, image, rich text, and a flexible query builder for data searching.
- **Easy Integration:** The Systems can be seamlessly integrated into your ASP.NET Core project via a NuGet package. You can extend your business logic by registering hook functions that execute before or after database access.
- **Performance:** The system is designed with performance in mind, boasting speeds 100 times faster than Strapi (details in the [performance vs Strapi](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-strapi.md) test). It is also as faster than manually written APIs using Entity Framework (details in the [performance vs EF](https://github.com/fluent-cms/fluent-cms/blob/main/doc%2Fpeformance-tests%2Fperformance-test-fluent-cms-vs-entity-framework.md) test).
- **Easily Extensible** The system can automatically generate `EntityCreated`, `EntityUpdated`, and `EntityDeleted` events and publish them to an event broker (such as Kafka). This makes it simple to extend functionality, such as adding consumers for OpenSearch, Elasticsearch, or document databases. 
## Live Demo - A blog website based on Fluent CMS 
   source code [FluentCMS.Blog](server%2FFluentCMS.Blog)
   - Admin Panel https://fluent-cms-admin.azurewebsites.net/
      - Email: `admin@cms.com`
      - Password: `Admin1!`  
   - Public Site : https://fluent-cms-ui.azurewebsites.net/
    
## Add Fluent CMS to your own project
The example project can be found at  https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples
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
   Currently FluentCMS support AddSqliteCms, AddSqlServerCms, AddPostgresCMS 

4. Add the following line After builder.Build()
   ```
   await app.UseCmsAsync();
   ```
   this function bootstrap router, initialize Fluent CMS schema table

Now you can start web server, the following chapter explains how to build schema and manage data.
## Develop a simple educational system use Fluent CMS
When designing a database schema for a simple educational system, you typically need to create tables for `Teachers`, `Courses`, and `Students`. The relationships between these tables can vary depending on the specific requirements, but a common structure might include the following:
### Database Schema
#### 1. **Teachers Table**
This table stores information about the teachers.

| Column Name     | Data Type  | Description                 |
|-----------------|------------|-----------------------------|
| `TeacherId`     | `INT`      | Primary Key, unique ID for each teacher. |
| `FirstName`     | `VARCHAR`  | Teacher's first name.        |
| `LastName`      | `VARCHAR`  | Teacher's last name.         |
| `Email`         | `VARCHAR`  | Teacher's email address.     |
| `PhoneNumber`   | `VARCHAR`  | Teacher's contact number.    |

#### 2. **Courses Table**
This table stores information about the courses.

| Column Name     | Data Type  | Description                   |
|-----------------|------------|-------------------------------|
| `CourseId`      | `INT`      | Primary Key, unique ID for each course. |
| `CourseName`    | `VARCHAR`  | Name of the course.            |
| `Description`   | `TEXT`     | Brief description of the course. |
| `TeacherId`     | `INT`      | Foreign Key, references `TeacherId` in the `Teachers` table. |

#### 3. **Students Table**
This table stores information about the students.

| Column Name     | Data Type  | Description                   |
|-----------------|------------|-------------------------------|
| `StudentId`     | `INT`      | Primary Key, unique ID for each student. |
| `FirstName`     | `VARCHAR`  | Student's first name.         |
| `LastName`      | `VARCHAR`  | Student's last name.          |
| `Email`         | `VARCHAR`  | Student's email address.      |
| `EnrollmentDate`| `DATE`     | Date when the student enrolled. |

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
After start your asp.net core application, you can a  menu item `Schema Builder` in the application's home page.
1. You can add entity `teacher`, `student` in schema builder UI
2. When you add entity `course`, after add basic attributes `name` and `description`, you can add relationships
   - add attribute `teacher`, with the following settings
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
   - add attribute `students`, with the following settings
   
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
Now the minimal value product is ready to use.
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
## Permissions

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
- Web Server: 
  - Code [FluentCMS](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS)
  - Doc [Server](https://github.com/fluent-cms/fluent-cms/blob/main/doc/Development.md#Server )
- Admin Panel Client:
  - Code [admin-panel](https://github.com/fluent-cms/fluent-cms/tree/main/admin-panel)
  - Doc [Admin-Panel-UI](https://github.com/fluent-cms/fluent-cms/blob/main/doc/Development.md#Admin-Panel-UI)
- Schema Builder: 
  - Code [schema-ui](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS/wwwroot/schema-ui)
  - Doc [Schema-Builder-UI](https://github.com/fluent-cms/fluent-cms/blob/main/doc/Development.md#Schema-Builder-UI)
