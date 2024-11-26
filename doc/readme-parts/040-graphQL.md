

## **GraphQL**

<details><summary>FluentCMS simplifies frontend development by offering robust GraphQL support.</summary>

### Accessing the GraphQL IDE
To get started, launch the web application and navigate to `/graph`. You can also try our [online demo](https://fluent-cms-admin.azurewebsites.net/graph).

### Singular vs. List Queries
For each entity in FluentCMS, two types of GraphQL queries are automatically generated:
- `<entityName>`: Returns a singular response.
- `<entityNameList>`: Returns a list of responses.

#### Examples:
**Single Course Query**
```graphql
{
  course {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20course%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**List of Courses Query**
```graphql
{
  courseList {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

### Field Selection
You can query specific fields of the current entity and related entities.

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

### Filtering Results - `in` Match
FluentCMS supports `in` filters using scalar values or lists.

**Match one value example Query:**
```graphql
{
  courseList(idSet: 5) {
    id
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A5)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

**Match list example Query:**
```graphql
{
  courseList(idSet:[5,7]){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(idSet%3A%5B5%2C7%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

### Filtering Result - specify match
In the following example filtering `id > 3 and id < 5`
```graphql
{
  courseList(id:{matchType:matchAll,gt:5,lt:15}){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(id%3A%7BmatchType%3AmatchAll%2Cgt%3A5%2Clt%3A15%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)


### Sorting Result 
Sorting by a single field
```
{
  courseList(sort:nameDesc){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3AnameDesc)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

Sorting by multiple fields
```
{
  courseList(sort:[level,id]){
    id,
    level
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(sort%3A%5Blevel%2Cid%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20level%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

### Pagination
Pagination on root field
```

```

Explore the power of FluentCMS GraphQL and streamline your development workflow!



#### 3. Limit
The query's `Page Size` property sets to `Limit clause`.
#### 4. Sorts
Sorts define the `order by` clause. Sorting can be applied to the main entity or related entities, e.g., below settings corresponds to:

```
select * from course left join teachers 
  on course.teacher = teachers.id
  order by teacher.firstname asc, course.id desc
```

```json
{
  "sorts": [
    {
      "fieldName": "teacher.firstname",
      "order": "Asc"
    },
    {
      "fieldName": "id",
      "order": "Desc"
    }
  ]
}
```

#### 5. Filter
Filter settings define the `WHERE` clause to control which data is retrieved.
- **fieldName**: Specifies a column in either the main entity or a related entity.
- **operator**: `and` requires all constraints to match, while `or` matches any single constraint.
- **constraints**:
  - **match**: Defines how to match values, such as `in` or `startWith`.
  - **value**: Can be hardcoded within the query or passed as a query string parameter.   
  For example, `qs.course_id` pulls the ID from the query string parameter `course_id`, where the prefix `qs.` indicates that the value comes from the query string.  
  Example API call: `/api/queries/<query-name>/one?course_id=3`  
  SQL equivalent: `SELECT * FROM courses WHERE id = 3`  
- **omitFail**: If the query string does not contain a specific constraint value, this option omits the filter. In the example below, if `skill_id` and `material_id` are not provided, FluentCMS ignores these filters.

```json
{
  "filters": [
    {
      "fieldName": "id",
      "operator": "and",
      "omitFail": true,
      "constraints": [
        {
          "match": "in",
          "value": "qs.course_id"
        }
      ]
    },
    {
      "fieldName": "materials.id",
      "operator": "and",
      "omitFail": true,
      "constraints": [
        {
          "match": "in",
          "value": "qs.material_id"
        }
      ]
    },
    {
      "fieldName": "teacher.skills.id",
      "operator": "and",
      "omitFail": true,
      "constraints": [
        {
          "match": "in",
          "value": "qs.skill_id"
        }
      ]
    }
  ]
}
```

#### 6. Join
When sorting or filtering by related entities, FluentCMS joins the relevant tables. For example, with `course` as the main entity and a filter like `teacher.skills.id`, FluentCMS will join these tables:

```
FROM "courses"
LEFT JOIN "teachers" AS "teacher" ON "courses"."teacher" = "teacher"."id"
LEFT JOIN "skill_teacher" AS "skills_skill_teacher" ON "teacher"."id" = "teacher_skills_skill_teacher"."teacher_id"
LEFT JOIN "skills" AS "teacher_skills" ON "teacher_skills_skill_teacher"."skill_id" = "teacher_skills"."id"
```

### Subfield

Subfields in GraphQL are useful for querying complex data structures and handling relationships between entities. For example, when querying for a list of courses, each course could include a nested `teacher` object. Within the `teacher`, additional subfields like `firstname`, `lastname`, and `bio` can be queried. Each level of nesting defines a new layer of subfields.

```graphql
{
    id
    name
    teacher {
        firstname
        lastname
        bio
        skills {
            name
            years
        }
    }
}
```

With FluentCMS, you can add `filter`, `sort`, `offset`, and `limit` options to subfields, allowing for even more control over returned data.

#### Filter in Subfields

Filtering can be specified in three forms:

1. **`fieldName:value` format**

   ```graphql
   firstname
   lastname
   skills(years: 3) {
       name
       years
   }
   ```

   Here, the argument `years: 3` for the `skills` subfield generates a query with a `WHERE` clause equivalent to `years = 3`.

2. **`fieldName: { match: value }` format**

   ```graphql
   firstname
   lastname
   skills(years: { gt: 3 }) {
       name
       years
   }
   ```

   Using `years: { gt: 3 }` as an argument for the `skills` subfield generates a `WHERE` clause `years > 3`.

3. **Advanced filter with multiple conditions**

   ```graphql
   firstname
   lastname
   skills(years: { equals: 3, equals: 4, operator: or }) {
       name
       years
   }
   ```

   With `years: { equals: 3, equals: 4, operator: or }`, the query service generates a `WHERE` clause `years = 3 OR years = 4`.

#### Sort in subfields

Sort arguments can be provided in two forms:

1. **Simple sort by field**

   ```graphql
   firstname
   lastname
   skills(years: 3, sort: years) {
       name
       years
   }
   ```

   With `sort: years`, the query service generates an `ORDER BY` clause like `ORDER BY years`.

2. **Sort by multiple fields and directions**

   ```graphql
   firstname
   lastname
   skills(years: 3, sort: { years: desc, name: asc }) {
       name
       years
   }
   ```

   Here, `sort: { years: desc, name: asc }` generates an `ORDER BY` clause `ORDER BY years DESC, name ASC`.

#### Offset and Limit in subfields

`offset` and `limit` can be passed as query string parameters to control pagination. For instance, calling `/api/queries/teacher` might return a set of results:

```json
[
  {
    "id": 3,
    "firstname": "Jane",
    "lastname": "Debuggins",
    "skills": [
      {
        "id": 13,
        "name": "Html",
        "years": 7,
        "teacher_id": 3,
        "cursor": "eyJ5ZWFycyI6NywiaWQiOjEzLCJzb3VyY2VJZCI6M30"
      },
      {
        "id": 2,
        "name": "C#",
        "years": 5,
        "teacher_id": 3,
        "cursor": null
      },
      {
        "id": 15,
        "name": "Database",
        "years": 4,
        "teacher_id": 3,
        "cursor": null
      },
      {
        "id": 1,
        "name": "C++",
        "years": 3,
        "teacher_id": 3,
        "cursor": "eyJ5ZWFycyI6MywiaWQiOjEsInNvdXJjZUlkIjozfQ"
      }
    ],
    "hasPreviousPage": false,
    "cursor": "eyJpZCI6M30"
  }
]
```

Calling `/api/queries/teacher?skills.limit=2&skills.offset=1` retrieves the second and third skills in the list:

```json
[
  {
    "id": 3,
    "firstname": "Jane",
    "lastname": "Debuggins",
    "skills": [
      {
        "id": 2,
        "name": "C#",
        "years": 5,
        "teacher_id": 3,
        "hasPreviousPage": false,
        "cursor": "eyJ5ZWFycyI6NSwiaWQiOjIsInNvdXJjZUlkIjozfQ"
      },
      {
        "id": 15,
        "name": "Database",
        "years": 4,
        "teacher_id": 3,
        "hasNextPage": true,
        "cursor": "eyJ5ZWFycyI6NCwiaWQiOjE1LCJzb3VyY2VJZCI6M30"
      }
    ],
    "hasPreviousPage": false,
    "cursor": "eyJpZCI6M30"
  }
]
```
#### Paginating subfields

Calling `/api/queries/teacher/part/skills?last=<cursor>` retrieves the records after the third skill.
```json
[
  {
    "id": 15,
    "name": "Database",
    "years": 4,
    "teacher_id": 3,
    "hasPreviousPage": true,
    "cursor": "eyJ5ZWFycyI6NCwiaWQiOjE1LCJzb3VyY2VJZCI6M30"
  },
  {
    "id": 1,
    "name": "C++",
    "years": 3,
    "teacher_id": 3,
    "hasNextPage": false,
    "cursor": "eyJ5ZWFycyI6MywiaWQiOjEsInNvdXJjZUlkIjozfQ"
  }
]
```
This setup allows precise control over filtering, sorting, and paginating nested data in FluentCMS.


### Query Endpoints

Each query has three corresponding endpoints:

- **List**: `/api/queries/<query-name>` retrieves a paginated list.
The example response is, if this page has previous page, the `hasPreviousPage` is set to ture. if the page has next page,
the `hasNextPage` is set to true. 

```json
[
  {
    "name": "The Art of Pizza Making",
    "id": 23,
    "hasPreviousPage": false,
    "cursor": "eyJpZCI6MjJ9"
  },
  {
    "name": "Functional Programming",
    "id": 22
  },
  {
    "id": 21,
    "name": "Functional Programming",
    "hasPreviousPage": true,
    "cursor": "eyJpZCI6M30"
  }
]
```
The cursor field value of the two edge items is used to fetch the next or previous page.
  - To view the next page: `/api/queries/<query-name>?last=<cursor>`
  - To view the previous page: `/api/queries/<query-name>?first=<cursor>`

`sorts` was applied when retrieving next page or previous page.


- **Single Record**: `/api/queries/<query-name>/one` returns the first record.
  - Example: `/api/queries/<query-name>/one?id=***`

- **Multiple Records**: `/api/queries/<query-name>/many` returns multiple records.
  - Example: `/api/queries/<query-name>/many?id=1&id=2&id=3`

If the number of IDs exceeds the page size, only the first set will be returned.

</details>