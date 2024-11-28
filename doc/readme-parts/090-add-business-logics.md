

## Add business logics 
<details>
<summary> 
The following chapter will guide you through add your own business logic by add validation logic, hook functions, and produce events to Kafka. 
</summary>

### Add validation logic using simple c# express

#### Simple C# logic
You can add simple c# expression to  `Validation Rule` of attributes, the expression is supported by [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso).  
For example, you can add simple expression like `name != null`.  
You can also add `Validation Error Message`, the end user can see this message if validate fail.

#### Regular Expression Support
`Dynamic Expresso` supports regex, for example you can write Validation Rule `Regex.IsMatch(email, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")`.   
Because `Dyamic Expresso` doesn't support [Verbatim String](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim), you have to escape `\`.

---
### Extent functionality by add Hook functions
You need to add your own Business logic, for examples, you want to verify if the email and phone number of entity `teacher` is valid.
you can register a cook function before insert or update teacher
```
var registry = app.GetHookRegistry();
registry.EntityPreAdd.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});
registry.EntityPreUpdate.Register("teacher", args =>
{
    VerifyTeacher(args.RefRecord);
    return args;
});

```

---
### Produce Events to Event Broker(e.g.Kafka)
You can also choose produce events to Event Broker(e.g.Kafka), so Consumer Application function can implement business logic in a async manner.
The producing event functionality is implemented by adding hook functions behind the scene,  to enable this functionality, you need add two line of code,
`builder.AddKafkaMessageProducer("localhost:9092");` and `app.RegisterMessageProducerHook()`.

```
builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
```
</details>