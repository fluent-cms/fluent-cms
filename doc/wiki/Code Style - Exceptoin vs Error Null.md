# ASP.NET Core App Error Handling - Returning Result vs Throwing Exceptions

In our ASP.NET Core application, we aim to avoid throwing exceptions for control flow due to the difficulties it introduces. Instead, we prefer returning `Result` objects using the [FluentResults](https://github.com/altmann/FluentResults) library.

However, to leverage ASP.NET Core's robust exception handling features, there are instances where we throw a customized exception—`InvalidParamException`. We rely on ASP.NET Core middleware to handle exceptions gracefully, so there's no need for explicit try-catch blocks.

## Returning Results

Consider the following code examples for handling query updates:

### Version 1

```csharp
public Result<Query?> GetUpdateQuery(Dictionary<string, object> item)
{
    return item.TryGetValue(PrimaryKey, out var val)
        ? Result.Ok(new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values))
        : null;
}
```

### Version 2

```csharp
public Result<Query> GetUpdateQuery(Dictionary<string, object> item)
{
    var ok = item.TryGetValue(PrimaryKey, out var val);
    if (!ok)
    {
        throw new Exception($"Failed to get value for primary key {PrimaryKey}");
    }
    return Result.Ok(new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values));
}
```

### Version 3

```csharp
public Result<Query> GetUpdateQuery(Dictionary<string, object> item)
{
    return item.TryGetValue(PrimaryKey, out var val)
        ? Result.Ok(new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values))
        : Result.Fail($"Failed to get value for primary key {PrimaryKey}");
}
```

### Analysis

- **Version 1**: Returning `null` makes it hard to debug when something goes wrong.
- **Version 2**: Throwing an exception makes the code look cluttered and forces the caller to use try-catch blocks.
- **Version 3**: Returning a `Result` object provides enough information for the caller to handle or ignore the error as needed.

## Using Exceptions in ASP.NET Core

ASP.NET Core has excellent support for exception handling. According to the official documentation:

- In development mode, detailed exception information is sent to the client.
- In production mode, only the `InvalidParamException` title is sent to the frontend.

### Error Handling Controller

```csharp
public class ErrorController : ControllerBase
{
    [Route("/error-development")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult HandleErrorDevelopment([FromServices] IHostEnvironment hostEnvironment)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error!;
        return ex is Services.InvalidParamException 
            ? Problem(title: ex.Message, detail: ex.StackTrace, statusCode: 400)
            : Problem(detail: ex.StackTrace, title: ex.Message);
    }

    [Route("/error")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult HandleError()
    {
        var ex = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error!;
        return ex is Services.InvalidParamException 
            ? Problem(title: ex.Message, statusCode: 400)
            : Problem();
    }
}
```

### Utility Function for Raising Exceptions

To keep the code concise, we use a utility function to raise exceptions:

```csharp
public static class InvalidParamExceptionFactory
{
    public static void CheckResult(Result? result)
    {
        if (result is not null && result.IsFailed)
        {
            throw new InvalidParamException($"{result.Errors}");
        }
    }
}
```

### Example Usage of `InvalidParamException`

```csharp
public async Task<int> Insert(string entityName, JsonElement ele)
{
    var entity = InvalidParamExceptionFactory.CheckResult(await schemaService.GetEntityByNameOrDefault(entityName));
    var record = RecordParser.Parse(ele, entity);
    return await queryKateQueryExecutor.Exec(entity.Insert(record));
}
```

By using `Result` objects and selectively throwing `InvalidParamException`, we maintain clean and readable code while leveraging ASP.NET Core's powerful exception handling capabilities.