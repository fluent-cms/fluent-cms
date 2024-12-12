# FluentCMS - a headless CMS with GraphQL and an drag-and-drop page designer.  

Welcome to [Fluent CMS](https://github.com/fluent-cms/fluent-cms)! [![GitHub stars](https://img.shields.io/github/stars/fluent-cms/fluent-cms.svg?style=social&label=Star)](https://github.com/fluent-cms/fluent-cms/stargazers)    
If you'd like to contribute to the project, please check out our [CONTRIBUTING guide](https://github.com/fluent-cms/fluent-cms/blob/main/CONTRIBUTING.md).Don’t forget to give us a star  ⭐ if you find Fluent CMS helpful!    
---
## What is Fluent CMS?

**Fluent CMS** is an open-source Content Management System designed to simplify and accelerate web development workflows. While it's particularly suited for CMS projects, it is also highly beneficial for general web applications, reducing the need for repetitive REST/GraphQL API development.

- **Effortless CRUD Operations:** Fluent CMS includes built-in RESTful APIs for Create, Read, Update, and Delete (CRUD) operations, complemented by a React-based admin panel for efficient data management.

- **Powerful GraphQL Queries:** Access multiple related entities in a single query, enhancing client-side performance, security, and flexibility.

- **Drag-and-Drop Page Designer:** Build dynamic pages effortlessly using the integrated page designer powered by [Grapes.js](https://grapesjs.com/) and [Handlebars](https://handlebarsjs.com/). Easily bind data sources for an interactive and streamlined design experience.

---
## Online Course System Demo

### Source Code
[Example Blog Project on GitHub](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples)

### Live Demo
- **Public Site:** [fluent-cms-admin.azurewebsites.net](https://fluent-cms-admin.azurewebsites.net/)
- **Admin Panel:** [fluent-cms-admin.azurewebsites.net/admin](https://fluent-cms-admin.azurewebsites.net/admin)
  - **Email:** `admin@cms.com`
  - **Password:** `Admin1!`

### Additional Resources
- **GraphQL Playground:** [fluent-cms-admin.azurewebsites.net/graph](https://fluent-cms-admin.azurewebsites.net/graph)
- **Documentation:** [fluent-cms-admin.azurewebsites.net/doc/index.html](https://fluent-cms-admin.azurewebsites.net/doc/index.html)  

---
## Online Course System Backend

<details> 
<summary> 
This section provides detailed guidance on developing a foundational online course system, encompassing key entities: `teacher`, `course`, `skill`, and `material`.
</summary>

### Database Schema

#### 1. **Teachers Table**
The `Teachers` table maintains information about instructors, including their personal and professional details.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `firstname`      | First Name       | String        |
| `lastname`       | Last Name        | String        |
| `email`          | Email            | String        |
| `phone_number`   | Phone Number     | String        |
| `image`          | Image            | String        |
| `bio`            | Bio              | Text          |

#### 2. **Courses Table**
The `Courses` table captures the details of educational offerings, including their scope, duration, and prerequisites.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Course Name      | String        |
| `status`         | Status           | String        |
| `level`          | Level            | String        |
| `summary`        | Summary          | String        |
| `image`          | Image            | String        |
| `desc`           | Description      | Text          |
| `duration`       | Duration         | String        |
| `start_date`     | Start Date       | Datetime      |

#### 3. **Skills Table**
The `Skills` table records competencies attributed to teachers.

| **Field**        | **Header**       | **Data Type** |
|-------------------|------------------|---------------|
| `id`             | ID               | Int           |
| `name`           | Skill Name       | String        |
| `years`          | Years of Experience | Int      |
| `created_by`     | Created By       | String        |
| `created_at`     | Created At       | Datetime      |
| `updated_at`     | Updated At       | Datetime      |

#### 4. **Materials Table**
The `Materials` table inventories resources linked to courses.

| **Field**        | **Header**  | **Data Type** |
|-------------------|-------------|---------------|
| `id`             | ID          | Int           |
| `name`           | Name        | String        |
| `type`           | Type        | String        |
| `image`          | Image       | String        |
| `link`           | Link        | String        |
| `file`           | File        | String        |

---

### Relationships
- **Teachers to Courses**: One-to-Many (Each teacher can teach multiple courses; each course is assigned to one teacher).
- **Teachers to Skills**: Many-to-Many (Multiple teachers can share skills, and one teacher may have multiple skills).
- **Courses to Materials**: Many-to-Many (A course may include multiple materials, and the same material can be used in different courses).

---

### Schema Creation via Fluent CMS Schema Builder

#### Accessing Schema Builder
After launching the web application, locate the **Schema Builder** menu on the homepage to start defining your schema.

#### Adding Entities
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/list.html?schema=entity)  
1. Navigate to the **Entities** section of the Schema Builder.
2. Create entities such as "Teacher" and "Course."
3. For the `Course` entity, add attributes such as `name`, `status`, `level`, and `description`.
---
### Defining Relationships
[Example Configuration](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/edit.html?schema=entity&id=27)  

#### 1. **Course and Teacher (Many-to-One Relationship)**
To establish a many-to-one relationship between the `Course` and `Teacher` entities, you can include a `Lookup` attribute in the `Course` entity. This allows selecting a single `Teacher` record when adding or updating a `Course`.

| **Attribute**  | **Value**    |
|----------------|--------------|
| **Field**      | `teacher`    |
| **Type**       | Lookup       |
| **Options**    | Teacher      |

**Description:** When a course is created or modified, a teacher record can be looked up and linked to the course.

#### 2. **Course and Materials (Many-to-Many Relationship)**
To establish a many-to-many relationship between the `Course` and `Material` entities, use a `Crosstable` attribute in the `Course` entity. This enables associating multiple materials with a single course.

| **Attribute**    | **Value**    |
|-------------------|--------------|
| **Field**         | `materials` |
| **Type**          | Crosstable  |
| **Options**       | Material    |

**Description:** When managing a course, you can select multiple material records from the `Material` table to associate with the course.

---

### Admin Panel: Data Management Features

#### 1. **List Page**
[Example Course List Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course?offset=0&limit=20)  
The **List Page** displays entities in a tabular format, enabling sorting, searching, and pagination. Users can efficiently browse or locate specific records.

#### 2. **Detail Page**
[Example Course Detail Page](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/admin/entities/course/22)  
The **Detail Page** provides an interface for viewing and managing detailed attributes. Related data such as teachers and materials can be selected or modified.

--- 
</details>
---
## **GraphQL Query**

<details>
<summary>
FluentCMS simplifies frontend development by offering robust GraphQL support.
</summary>

### Getting Started
#### Accessing the GraphQL IDE
To get started, launch the web application and navigate to `/graph`. You can also try our [online demo](https://fluent-cms-admin.azurewebsites.net/graph).

---
#### Singular vs. List Response
For each entity in FluentCMS, two GraphQL fields are automatically generated:  
- `<entityName>`: Returns a record.
- `<entityNameList>`: Returns a list of records.  

**Single Course **
```graphql
{
  course {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20course%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**List of Courses **
```graphql
{
  courseList {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---
#### Field Selection
You can query specific fields for both the current entity and its related entities.
**Example Query:**
```graphql
{
  courseList{
    id
    name
    teacher{
      id
      firstname
      lastname
      skills{
        id
        name
      }
    }
    materials{
      id,
      name
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20firstname%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%20%20skills%7B%0A%20%20%20%20%20%20%20%20id%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%20%20%20%20materials%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D%0A)

---
#### Filtering with `Value Match` in FluentCMS

FluentCMS provides flexible filtering capabilities using the `idSet` field (or any other field), enabling precise data queries by matching either a single value or a list of values.

**Filter by a Single Value Example:**
```graphql
{
  courseList(idSet: 5) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A5)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**Filter by Multiple Values Example:**
```graphql
{
  courseList(idSet: [5, 7]) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A%5B5%2C7%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---
#### Advanced Filtering with `Operator Match` in FluentCMS

FluentCMS supports advanced filtering options with `Operator Match`, allowing users to combine various conditions for precise queries.

##### `matchAll` Example:
Filters where all specified conditions must be true.  
In this example: `id > 5 and id < 15`.

```graphql
{
  courseList(id: {matchType: matchAll, gt: 5, lt: 15}) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(id%3A%7BmatchType%3AmatchAll%2Cgt%3A5%2Clt%3A15%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

##### `matchAny` Example:
Filters where at least one of the conditions must be true.  
In this example: `name starts with "A"` or `name starts with "I"`.

```graphql
{
  courseList(name: [{matchType: matchAny}, {startsWith: "A"}, {startsWith: "I"}]) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(name%3A%5B%7BmatchType%3AmatchAny%7D%2C%20%7BstartsWith%3A%22A%22%7D%2C%7BstartsWith%3A%22I%22%7D%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)


---

#### `Filter Expressions` in FluentCMS

Filter Expressions allow precise filtering by specifying a field, including nested fields using JSON path syntax. This enables filtering on subfields for complex data structures.

***Example: Filter by Teacher's Last Name***
This query returns courses taught by a teacher whose last name is "Circuit."

```graphql
{
  courseList(filterExpr: {field: "teacher.lastname", clause: {equals: "Circuit"}}) {
    id
    name
    teacher {
      id
      lastname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(filterExpr%3A%20%7Bfield%3A%20%22teacher.lastname%22%2C%20clause%3A%20%7Bequals%3A%20%22Circuit%22%7D%7D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%20%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D)

---

#### Sorting  
Sorting by a single field
```graphql
{
  courseList(sort:nameDesc){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3AnameDesc)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

Sorting by multiple fields
```graphql
{
  courseList(sort:[level,id]){
    id,
    level
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3A%5Blevel%2Cid%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20level%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

---

#### Sort Expressions in FluentCMS


Sort Expressions allow sorting by nested fields using JSON path syntax. 

***Example: Sort by Teacher's Last Name***

```graphql
{
  courseList(sortExpr:{field:"teacher.lastname", order:Desc}) {
    id
    name
    teacher {
      id
      lastname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sortExpr%3A%7Bfield%3A%22teacher.lastname%22%2C%20order%3ADesc%7D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20name%0A%20%20%20%20teacher%20%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20lastname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D)

---

#### Pagination
Pagination on root field
```graphql
{
  courseList(offset:2, limit:3){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%20%20%7B%0A%20%20%20%20courseList(offset%3A2%2C%20limit%3A3)%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A)   
Pagination on sub field
```graphql
{
  courseList{
    id,
    name
    materials(limit:2){
      id,
      name
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%20%20%7B%0A%20%20%20%20courseList%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20materials(limit%3A2)%7B%0A%20%20%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%20%20%7D%0A)

---



#### Variable

Variables are used to make queries more dynamic, reusable, and secure.
##### Variable in `Value filter`
```
query ($id: Int!) {
  teacher(idSet: [$id]) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacher(idSet%3A%20%5B%24id%5D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22id%22%3A3%0A%7D)

##### Variable in `Operator Match` filter
```
query ($id: Int!) {
  teacherList(id:{equals:$id}){
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacherList(id%3A%7Bequals%3A%24id%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22id%22%3A%203%0A%7D)

##### Variable in `Filter Expression`
```
query ($years: String) {
  teacherList(filterExpr:{field:"skills.years",clause:{gt:$years}}){
    id
    firstname
    lastname
    skills{
      id
      name
      years
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24years%3A%20String)%20%7B%0A%20%20teacherList(filterExpr%3A%7Bfield%3A%22skills.years%22%2Cclause%3A%7Bgt%3A%24years%7D%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%20%20skills%7B%0A%20%20%20%20%20%20id%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20years%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22years%22%3A%20%229%22%0A%7D)

##### Variable in Sort 
```
query ($sort_field:TeacherSortEnum) {
  teacherList(sort:[$sort_field]) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24sort_field%3ATeacherSortEnum)%20%7B%0A%20%20teacherList(sort%3A%5B%24sort_field%5D)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%22sort_field%22%3A%20%22idDesc%22%7D)
##### Variable in Sort Expression
```
query ($sort_order:  SortOrderEnum) {
  courseList(sortExpr:{field:"teacher.id", order:$sort_order}){
    id,
    name,
    teacher{
      id,
      firstname
    }
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24sort_order%3A%20%20SortOrderEnum)%20%7B%0A%20%20courseList(sortExpr%3A%7Bfield%3A%22teacher.id%22%2C%20order%3A%24sort_order%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%2C%0A%20%20%20%20teacher%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20firstname%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&variables=%7B%0A%20%20%22sort_order%22%3A%20%22Asc%22%0A%7D)

##### Variable in Pagination
```
query ($offset:Int) {
  teacherList(limit:2, offset:$offset) {
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24offset%3AInt)%20%7B%0A%20%20teacherList(limit%3A2%2C%20offset%3A%24offset)%20%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D&variables=%7B%0A%09%22offset%22%3A%202%0A%7D)

---

#### Required vs Optional
If you want a variable to be mandatory, you can add a  `!` to the end of the type
```
query ($id: Int!) {
  teacherList(id:{equals:$id}){
    id
    firstname
    lastname
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20(%24id%3A%20Int!)%20%7B%0A%20%20teacherList(id%3A%7Bequals%3A%24id%7D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%7D%0A%7D)

Explore the power of FluentCMS GraphQL and streamline your development workflow!
***
***
### Saved Query

**Realtime queries** may expose excessive technical details, potentially leading to security vulnerabilities.

**Saved Queries** address this issue by abstracting the GraphQL query details. They allow clients to provide only variables, enhancing security while retaining full functionality.

---

#### Transitioning from **Real-Time Queries** to **Saved Queries**

##### Using `OperationName` as the Saved Query Identifier
In FluentCMS, the **Operation Name** in a GraphQL query serves as a unique identifier for saved queries. For instance, executing the following query automatically saves it as `TeacherQuery`:

```graphql
query TeacherQuery($id: Int) {
  teacherList(idSet: [$id]) {
    id
    firstname
    lastname
    skills {
      id
      name
    }
  }
}
```

[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=query%20TeacherQuery(%24id%3A%20Int)%20%7B%0A%20%20teacherList(idSet%3A%5B%24id%5D)%7B%0A%20%20%20%20id%0A%20%20%20%20firstname%0A%20%20%20%20lastname%0A%20%20%20%20skills%7B%0A%20%20%20%20%20%20id%2C%0A%20%20%20%20%20%20name%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&operationName=TeacherQuery)

---

##### Saved Query Endpoints
FluentCMS generates two API endpoints for each saved query:

1. **List Records:**  
   [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery)

2. **Single Record:**  
   [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/one/](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/one)

---

##### Using REST API Query Strings as Variables
The Saved Query API allows passing variables via query strings:

- **Single Value:**  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/?id=3](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/?id=3)

- **Array of Values:**  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?id=3&id=4](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?id=3&id=4)  
  This passes `[3, 4]` to the `idSet` argument.

---

#### Additional Features of `Saved Query`

Beyond performance and security improvements, `Saved Query` introduces enhanced functionalities to simplify development workflows.

---

##### Pagination by `offset`
Built-in variables `offset` and `limit` enable efficient pagination. For example:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=2&offset=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=2&offset=2)

---

##### `offset` Pagination for Subfields
To display a limited number of subfield items (e.g., the first two skills of a teacher), use the JSON path variable, such as `skills.limit`:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2)

---

##### Pagination by `cursor`
For large datasets, `offset` pagination can strain the database. For example, querying with `offset=1000&limit=10` forces the database to retrieve 1010 records and discard the first 1000.

To address this, `Saved Query` supports **cursor-based pagination**, which reduces database overhead.  
Example response for [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3):

```json
[
  {
    "hasPreviousPage": false,
    "cursor": "eyJpZCI6M30"
  },
  {
  },
  {
    "hasNextPage": true,
    "cursor": "eyJpZCI6NX0"
  }
]
```

- If `hasNextPage` of the last record is `true`, use the cursor to retrieve the next page:  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&last=eyJpZCI6NX0](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&last=eyJpZCI6NX0)

- Similarly, if `hasPreviousPage` of the first record is `true`, use the cursor to retrieve the previous page:  
  [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&first=eyJpZCI6Nn0](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?limit=3&first=eyJpZCI6Nn0)

---

##### Cursor-Based Pagination for Subfields
Subfields also support cursor-based pagination. For instance, querying [https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery?skills.limit=2) returns a response like this:

```json
[
  {
    "id": 3,
    "firstname": "Jane",
    "lastname": "Debuggins",
    "hasPreviousPage": false,
    "skills": [
      {
        "hasPreviousPage": false,
        "cursor": "eyJpZCI6MSwic291cmNlSWQiOjN9"
      },
      {
        "hasNextPage": true,
        "cursor": "eyJpZCI6Miwic291cmNlSWQiOjN9"
      }
    ],
    "cursor": "eyJpZCI6M30"
  }
]
```

To fetch the next two skills, use the cursor:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/part/skills?limit=2&last=eyJpZCI6Miwic291cmNlSWQiOjN9](https://fluent-cms-admin.azurewebsites.net/api/queries/TeacherQuery/part/skills?limit=2&last=eyJpZCI6Miwic291cmNlSWQiOjN9)

</details>

---
## Drag and Drop Page Designer
<details> 
<summary> 
The page designer utilizes the open-source GrapesJS and Handlebars, enabling seamless binding of `GrapesJS Components` with `FluentCMS Queries` for dynamic content rendering. 
</summary>

---
### Page Types: Landing Page, Detail Page, and Home Page

#### **Landing Page**
A landing page is typically the first page a visitor sees.  
- **URL format**: `/page/<pagename>`  
- **Structure**: Comprised of multiple sections, each section retrieves data via a `query`.  

**Example**:    
[Landing Page](https://fluent-cms-admin.azurewebsites.net/)    
This page fetches data from:  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured)  
- [https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced)  

---

#### **Detail Page**
A detail page provides specific information about an item.  
- **URL format**: `/page/<pagename>/<router parameter>`  
- **Data Retrieval**: FluentCMS fetches data by passing the router parameter to a `query`.  

**Example**:  
[Course Detail Page](https://fluent-cms-admin.azurewebsites.net/pages/course/22)  
This page fetches data from:  
[https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22](https://fluent-cms-admin.azurewebsites.net/api/queries/course/one?course_id=22)

---

#### **Home Page**
The homepage is a special type of landing page named `home`.  
- **URL format**: `/pages/home`   
- **Special Behavior**: If no other route matches the path `/`, FluentCMS renders `/pages/home` by default.  

**Example**:    
The URL `/` will be resolved to `/pages/home` unless explicitly overridden.  

---
### Introduction to GrapesJS Panels

Understanding the panels in GrapesJS is crucial for leveraging FluentCMS's customization capabilities in the Page Designer UI. This section explains the purpose of each panel and highlights how FluentCMS enhances specific areas to streamline content management and page design. 

![GrapesJS Toolbox](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/grapes-toolbox.png)

1. **Style Manager**:
    - Used to customize CSS properties of elements selected on the canvas.
    - *FluentCMS Integration*: This panel is left unchanged by FluentCMS, as it already provides powerful styling options.

2. **Traits Panel**:
    - Allows modification of attributes for selected elements.
    - *FluentCMS Integration*: Custom traits are added to this panel, enabling users to bind data to components dynamically.

3. **Layers Panel**:
    - Displays a hierarchical view of elements on the page, resembling a DOM tree.
    - *FluentCMS Integration*: While FluentCMS does not alter this panel, it’s helpful for locating and managing FluentCMS blocks within complex page designs.

4. **Blocks Panel**:
    - Contains pre-made components that can be dragged and dropped onto the page.
    - *FluentCMS Integration*: FluentCMS enhances this panel by adding custom-designed blocks tailored for its CMS functionality.

By familiarizing users with these panels and their integration points, this chapter ensures a smoother workflow and better utilization of FluentCMS's advanced page-building tools.

---
### Data Binding: Singleton or List

FluentCMS leverages [Handlebars expressions](https://github.com/Handlebars-Net/Handlebars.Net) for dynamic data binding in pages and components.

---

#### **Singleton**

Singleton fields are enclosed within `{{ }}` to dynamically bind individual values.

- **Example Page Settings:** [Page Schema Settings](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/page.html?schema=page&id=33)
- **Example Query:** [Retrieve Course Data](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?course_id=22)
- **Example Rendered Page:** [Rendered Course Page](https://fluent-cms-admin.azurewebsites.net/pages/course/22)

---

#### **List**

`Handlebars` supports iterating over arrays using the `{{#each}}` block for repeating data structures.

```handlebars
{{#each course}}
    <li>{{title}}</li>
{{/each}}
```

In FluentCMS, you won’t explicitly see the `{{#each}}` statement in the Page Designer. If a block's data source is set to `data-list`, FluentCMS automatically generates the loop.

- **Example Page Settings:** [Page Schema Settings](https://fluent-cms-admin.azurewebsites.net/_content/FluentCMS/schema-ui/page.html?schema=page&id=32)
- **Example Rendered Page:** [Rendered List Page](https://fluent-cms-admin.azurewebsites.net/)
- **Example Queries:**
   - [Featured Courses](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?status=featured)
   - [Advanced Level Courses](https://fluent-cms-admin.azurewebsites.net/api/queries/course/?level=Advanced)

---

#### **Steps to Bind a Data Source**

To bind a `Data List` to a component, follow these steps:

1. Drag a block from the **Data List** category in the Page Designer.
2. Open the **Layers Panel** and select the `Data List` component.
3. In the **Traits Panel**, configure the following fields:

| **Field**     | **Description**                                                                                                                                                        |
|---------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Query**     | The query to retrieve data.                                                                                                                                           |
| **Qs**        | Query string parameters to pass (e.g., `?status=featured`, `?level=Advanced`).                                                                                        |
| **Offset**    | Number of records to skip.                                                                                                                                            |
| **Limit**     | Number of records to retrieve.                                                                                                                                        |
| **Pagination**| Options for displaying content:                                                                                                                                       |
|               | - **Button**: Divides content into multiple pages with navigation buttons (e.g., "Next," "Previous," or numbered buttons).                                            |
|               | - **Infinite Scroll**: Automatically loads more content as users scroll. Ideal for a single component at the bottom of the page.                                      |
|               | - **None**: Displays all available content at once without requiring additional user actions.                                                                          |

</details>
---
## Online Course System Frontend
<details> 
<summary> 
Having established our understanding of Fluent CMS essentials like Entity, Query, and Page, we're ready to build a frontend for an online course website.
</summary>

---
### Key Pages

- **Home Page (`home`)**: The main entry point, featuring sections like *Featured Courses* and *Advanced Courses*. Each course links to its respective **Course Details** page.
- **Course Details (`course/{course_id}`)**: Offers detailed information about a specific course and includes links to the **Teacher Details** page.
- **Teacher Details (`teacher/{teacher_id}`)**: Highlights the instructor’s profile and includes a section displaying their latest courses, which link back to the **Course Details** page.


```plaintext
             Home Page
                 |
                 |
       +-------------------+
       |                   |
       v                   v
 Latest Courses       Course Details 
       |                   |        
       |                   |       
       v                   v            
Course Details <-------> Teacher Details 
```

---

### Designing the Home Page

1. **Drag and Drop Components**: Use the Fluent CMS page designer to drag a `Content-B` component.
2. **Set Data Source**: Assign the component's data source to the `course` query.
3. **Link Course Items**: Configure the link for each course to `/pages/course/{{id}}`. The Handlebars expression `{{id}}` is dynamically replaced with the actual course ID during rendering.

![Link Example](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-link.png)

---

### Creating the Course Details Page

1. **Page Setup**: Name the page `course/{course_id}` to capture the `course_id` parameter from the URL (e.g., `/pages/course/20`).
2. **Query Configuration**: The variable `{course_id:20}` is passed to the `course` query, generating a `WHERE id IN (20)` clause to fetch the relevant course data.
3. **Linking to Teacher Details**: Configure the link for each teacher item on this page to `/pages/teacher/{{teacher.id}}`. Handlebars dynamically replaces `{{teacher.id}}` with the teacher’s ID. For example, if a teacher object has an ID of 3, the link renders as `/pages/teacher/3`.

---

### Creating the Teacher Details Page

1. **Page Setup**: Define the page as `teacher/{teacher_id}` to capture the `teacher_id` parameter from the URL.
2. **Set Data Source**: Assign the `teacher` query as the page’s data source.

#### Adding a Teacher’s Courses Section

- Drag a `ECommerce A` component onto the page.
- Set its data source to the `course` query, filtered by the teacher’s ID (`WHERE teacher IN (3)`).

![Teacher Page Designer](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/screenshots/designer-teacher.png)

When rendering the page, the `PageService` automatically passes the `teacher_id` (e.g., `{teacher_id: 3}`) to the query.
</details>  

---
## Optimizing Caching
<details>
<summary>
To enhance performance, Fluent CMS implements caching strategies. 
</summary>

### Cache Types

1. **Entity Definition Cache**  
   Fluent CMS requires caching of all entity definitions to dynamically generate GraphQL types.

2. **Query Definition Cache**  
   Each query may depend on multiple related entities. Fluent CMS caches these definitions to compose efficient SQL queries.

---

### IMemoryCache in Fluent CMS

By default, Fluent CMS utilizes ASP.NET's `IMemoryCache` for caching.

- **Advantages**:
    - Simple to debug and deploy.
    - Suitable for single-node web applications.

- **Disadvantages**:
    - Not scalable for distributed environments. In multi-node deployments, cache invalidation on one node (e.g., Node A) does not propagate to other nodes (e.g., Node B).

---

### HybridCache for Scalable Caching

Starting with ASP.NET 9.0, the framework provides `HybridCache`, which combines a primary memory cache with a secondary external cache (e.g., Redis).

- **Key Features**:
    - **Scalability**: Combines the performance of local memory caching with the distributed consistency of external caching.
    - **Stampede Resolution**: The `HybridCache` resolves cache stampede issues, as confirmed by its developers.

- **Limitations**:  
  The current implementation lacks "Backend-Assisted Local Cache Invalidation," which means cache invalidation on one node does not immediately propagate to others.

- **Fluent CMS Strategy**:  
  To address this, Fluent CMS sets local cache expiration (20 seconds) to one-third of the distributed cache expiration (60 seconds). This ensures memory caches across nodes achieve consistency within 20 seconds, significantly improving over a standard memory cache's 60-second delay.

</details>

---
## Aspire Integration
<details> 
<summary> 
A scalable deployment of Fluent CMS involves multiple web application nodes, a Redis server for distributed caching, and a database server, all behind a load balancer.
</summary>

### Architecture Overview

```
                 +------------------+
                 |  Load Balancer   |
                 +------------------+
                          |
        +-----------------+-----------------+
        |                                   |
+------------------+              +------------------+
|    Web App 1     |              |    Web App 2     |
|   +-----------+  |              |   +-----------+  |
|   | Local Cache| |              |   | Local Cache| |
+------------------+              +------------------+
        |                                   |
        |                                   |
        +-----------------+-----------------+
                 |                       |
       +------------------+    +------------------+
       | Database Server  |    |   Redis Server   |
       +------------------+    +------------------+
```

---

### Local Emulation with Aspire and Service Discovery

[Example Web project on GitHub](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS.Blog)  
[Example Aspire project on GitHub](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS.Blog.AppHost)  

To emulate the production environment locally, Fluent CMS leverages Aspire. Here's an example setup:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Adding Redis and PostgreSQL services
var redis = builder.AddRedis(name: CmsConstants.Redis);
var db = builder.AddPostgres(CmsConstants.Postgres);

// Configuring the web project with replicas and references
builder.AddProject<Projects.FluentCMS_Blog>(name: "web")
    .WithEnvironment(CmsConstants.DatabaseProvider, CmsConstants.Postgres)
    .WithReference(redis)
    .WithReference(db)
    .WithReplicas(2);

builder.Build().Run();
```

### Benefits:
1. **Simplified Configuration**:  
   No need to manually specify endpoints for the database or Redis servers. Configuration values can be retrieved using:
   ```csharp
   builder.Configuration.GetValue<string>();
   builder.Configuration.GetConnectionString();
   ```
2. **Realistic Testing**:  
   The local environment mirrors the production architecture, ensuring seamless transitions during deployment.

By adopting these caching and deployment strategies, Fluent CMS ensures improved performance, scalability, and ease of configuration.
</details>

---
## Integrating it into Your Project

<details>
<summary>
Follow these steps to integrate Fluent CMS into your project using a NuGet package.
</summary>

1. **Create a New ASP.NET Core Web Application**.

2. **Add the FluentCMS NuGet Package**:
   To add Fluent CMS, run the following command:  
   ```
   dotnet add package FluentCMS
   ```

3. **Modify `Program.cs`**:
   Add the following line before `builder.Build()` to configure the database connection (use your actual connection string):  
   ```
   builder.AddSqliteCms("Data Source=cms.db");
   var app = builder.Build();
   ```

   Currently, Fluent CMS supports `AddSqliteCms`, `AddSqlServerCms`, and `AddPostgresCms`.

4. **Initialize Fluent CMS**:
   Add this line after `builder.Build()` to initialize the CMS:  
   ```
   await app.UseCmsAsync();
   ```  
   This will bootstrap the router and initialize the Fluent CMS schema table.

5. **Optional: Set Up User Authorization**:
   If you wish to manage user authorization, you can add the following code. If you're handling authorization yourself or don’t need it, you can skip this step.  
   ```
   builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
   builder.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();
   ```

   If you'd like to create a default user, add this after `app.Build()`:
   ```
   InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]));
   ```

Once your web server is running, you can access the **Admin Panel** at `/admin` and the **Schema Builder** at `/schema`.

You can find an example project [here](https://github.com/fluent-cms/fluent-cms/tree/main/examples/WebApiExamples).

</details>
---
## Adding Business Logic

<details>
<summary>
Learn how to customize your application by adding validation logic, hook functions, and producing events to Kafka.
</summary>

### Adding Validation Logic with Simple C# Expressions

#### Simple C# Validation
You can define simple C# expressions in the `Validation Rule` of attributes using [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso). For example, a rule like `name != null` ensures the `name` attribute is not null.

Additionally, you can specify a `Validation Error Message` to provide users with feedback when validation fails.

#### Using Regular Expressions
`Dynamic Expresso` supports regular expressions, allowing you to write rules like `Regex.IsMatch(email, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")`.

> Note: Since `Dynamic Expresso` doesn't support [verbatim strings](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim), you must escape backslashes (`\`).

---

### Extending Functionality with Hook Functions

To implement custom business logic, such as verifying that a `teacher` entity has valid email and phone details, you can register hook functions to run before adding or updating records:

```csharp
var registry = app.GetHookRegistry();

// Hook function for pre-add validation
registry.EntityPreAdd.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});

// Hook function for pre-update validation
registry.EntityPreUpdate.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});
```

---

### Producing Events to an Event Broker (e.g., Kafka)

To enable asynchronous business logic through an event broker like Kafka, you can produce events using hook functions. This feature requires just a few additional setup steps:

1. Add the Kafka producer configuration:
   ```csharp
   builder.AddKafkaMessageProducer("localhost:9092");
   ```

2. Register the message producer hook:
   ```csharp
   app.RegisterMessageProducerHook();
   ```

Here’s a complete example:

```csharp
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```

With this setup, events are produced to Kafka, allowing consumers to process business logic asynchronously.

</details>

---
## Development Guide
<details>
<summary>
The backend is written in ASP.NET Core, the Admin Panel uses React, and the Schema Builder is developed with jQuery.
</summary>


### System Overviews
![System Overview](https://raw.githubusercontent.com/fluent-cms/fluent-cms/doc/doc/diagrams/overview.png)   
- [**Backend Server**](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS)  
- [**Admin Panel UI**](https://github.com/fluent-cms/fluent-cms/tree/main/admin-panel)  
- [**Schema Builder**](https://github.com/fluent-cms/fluent-cms/tree/main/server/FluentCMS/wwwroot/schema-ui)  

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


---
## Testing Strategy
<details>
<summary>
This chapter describes Fluent CMS's automated testing strategy
</summary>

Fluent CMS favors integration testing over unit testing because integration tests can catch more real-world issues. For example, when inserting a record into the database, multiple modules are involved:
- `EntitiesController`
- `EntitiesService`
- `Entity` (in the query builder)
- Query executors (e.g., `SqlLite`, `Postgres`, `SqlServer`)

Writing unit tests for each individual function and mocking its upstream and downstream services can be tedious. Instead, Fluent CMS focuses on checking the input and output of RESTful API endpoints in its integration tests.

However, certain cases, such as the Hook Registry or application bootstrap, are simpler to cover with unit tests.

### Unit Testing `/fluent-cms/server/FluentCMS.Test`
This project focuses on testing specific modules, such as:
- Hook Registry
- Application Bootstrap

### Integration Testing for FluentCMS.Blog `/fluent-cms/server/FluentCMS.Blog.Tests`
This project focuses on verifying the functionalities of the FluentCMS.Blog example project.

### New Feature Testing `/fluent-cms/server/FluentCMS.App.Tests`
This project is dedicated to testing experimental functionalities, like MongoDB and Kafka plugins.

</details>