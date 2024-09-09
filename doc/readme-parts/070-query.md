
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
