

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