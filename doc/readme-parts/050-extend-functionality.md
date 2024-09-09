
## Adding your own business logics 
<details>
  <summary>
      The following chapter will guide you through add your own business logic by add validation logic, hook functions, and produce events to Kafka.
  </summary>

### Add validation logic using simple c# express
You can add simple c# expression to  `Validation Rule` of attributes, the expression is supported by [Dynamic Expresso](https://github.com/dynamicexpresso/DynamicExpresso).
for example you can add simple expression like `name != null`.

### Extent functionality by add Hook functions
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

### Produce Events to Kafka
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