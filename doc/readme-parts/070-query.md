

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