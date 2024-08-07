# Asp.net Core Web Application - UI Test and Integration Test

As the project supports multiple database platforms, it is essential to implement test automation. We conduct two types of tests: unit tests and integration tests. However, we prefer integration tests over unit tests for the following reasons:

1. **Simple Functions**: Some functions are straightforward, and preparing test data and mocking dependencies isn't worth the effort. For instance, a function that formats a date string might not need extensive unit testing. An integration test that verifies the formatted date within the context of a larger process is more valuable.

2. **Complex Interactions**: Certain functions are highly complex, involving numerous interacting parts. Unit tests cannot ensure the integrity of such a complex system. For example, a function that processes user transactions might interact with multiple services and databases. Integration tests are better suited to validate the entire workflow and interactions.

3. **Real-World Issues**: Unit tests often cannot identify real-world issues, such as database connection failures or data duplications. For instance, a unit test might pass for a data retrieval function, but an integration test could reveal problems like connection timeouts or duplicate data entries in a real database environment.

## Unit Test
We implement unit test in the project  [FluentCMS.Tests](..%2F..%2Fserver%2FFluentCMS.Tests)

The target service SchemaService is ideal for unit test because:
- It's easy to mock its dependencies. we can easily mock up it's date layer dependency `SqliteDefinitionExecutor` and `SqliteKateProvider`.
- The logic of SchemaService is worth the efforts to test.

We use sqlite because we can easily create a new database, delete it after test each time. sqlite is better than in-memory database because if the test fails,
We can look at the data to locate the root cause.

To ensure we use a new database each time, the connection string is write as follow.
```
private readonly string _dbFilePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.db");

```

The test class implements IAsyncLifeTime, so we can make sure the new file is deleted if the test finishes.
```
public class SchemaServiceTests :IAsyncLifetime{
    ...
    public Task DisposeAsync()
    {
        CleanDb();
        return Task.CompletedTask;
    }
}
```
## Integration Test
We have two integration Test,  
- [FluentCMS.Blog.Tests](..%2F..%2Fserver%2FFluentCMS.Blog.Tests) tests a basic workflow of from create schema to data CRUD.
- [FluentCMS.App.Tests](..%2F..%2Fserver%2FFluentCMS.App.Tests) tests how to implement user's own business logic using hook functions.

### Using Integration Test to Test Multiple Database Platform
We want fluentCMS works on the following scenarios
- Sqlite with empty database
- Sqlite with demo database
- Postgres with empty database
- Postgres with demo database
- SqlServer with empty database

To keep the test code simple, we use shell scripts [IntegrationTest.sh](..%2F..%2Fserver%2FFluentCMS.Blog.Tests%2FIntegrationTest.sh)