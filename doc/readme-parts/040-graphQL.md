

## **GraphQL**

<details><summary>FluentCMS simplifies frontend development by offering robust GraphQL support.</summary>

### Only the fly GraphQL
#### Accessing the GraphQL IDE
To get started, launch the web application and navigate to `/graph`. You can also try our [online demo](https://fluent-cms-admin.azurewebsites.net/graph).

#### Singular vs. List Queries
For each entity in FluentCMS, two types of GraphQL queries are automatically generated:
- `<entityName>`: Returns a singular response.
- `<entityNameList>`: Returns a list of responses.

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

#### Field Selection
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

#### Filtering Results - `in` 
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

#### Filtering Result - specify match
matchAll, In the following example filtering `id > 3 and id < 5`
```graphql
{
  courseList(id:{matchType:matchAll,gt:5,lt:15}){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(id%3A%7BmatchType%3AmatchAll%2Cgt%3A5%2Clt%3A15%7D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

matchAny, filtering `name like 'a%' or name like 'b%'`
```graphql
{
  courseList(name:[{matchType:matchAny}, {startsWith:"A"},{startsWith:"I"}]){
    id,
    name
  }
}
```
[Try it here](https://fluent-cms-admin.azurewebsites.net/graph?query=%7B%0A%20%20courseList(name%3A%5B%7BmatchType%3AmatchAny%7D%2C%20%7BstartsWith%3A%22A%22%7D%2C%7BstartsWith%3A%22I%22%7D%5D)%7B%0A%20%20%20%20id%2C%0A%20%20%20%20name%0A%20%20%7D%0A%7D%0A)

#### Sorting Result 
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
#### Filter on sub field
query all course taught by teacher "Circuit"
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

#### sort on sub field
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

#### variable
Variables are used to make queries more dynamic, reusable, and secure.
***filter variable***
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

***pagination variable***
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
***Sort field variable***


required,
optional



Explore the power of FluentCMS GraphQL and streamline your development workflow!

### Saved Query

#### why save, cache improve performance, hide field details, only expose necessary variable

#### Operation Name as query name

#### overwrite variable, filter, sort, pagination

#### paginating by cursor

#### paginating sub field by cursor

#### Query Endpoints

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