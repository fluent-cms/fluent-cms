using FluentCMS.App;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.WebAppExt;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TestService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.AddSqliteCms("Data Source=cmsapp.db");
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync();
app.RegisterMessageProducerHook();

using var scope = app.Services.CreateScope();

var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();
var entity = await schemaService.GetEntityByNameOrDefault(TestEntity.EntityName);
if (entity.IsFailed)
{
    await schemaService.AddOrSaveSimpleEntity(TestEntity.EntityName, TestEntity.FieldName, "", "");
    var entityService = scope.ServiceProvider.GetRequiredService<IEntityService>();
    await entityService.Insert(TestEntity.EntityName, new Dictionary<string, object>
    {
        { TestEntity.FieldName, TestEntity.TestValue }
    });
}

RegisterHooks();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();

return;
void RegisterHooks()
{

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.BeforeInsert], 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeInsert"; });
    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.AfterInsert], 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "AfterInsert"; });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.BeforeUpdate], 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeUpdate"; });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.AfterUpdate], 
        (IDictionary<string,object>test) => { test[TestEntity.FieldName] += "AfterUpdate"; });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.BeforeDelete], 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeDelete"; });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.AfterDelete], 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "AfterDelete"; });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.BeforeQueryOne], (EntityMeta meta) =>
    {
        if (meta.Id == "1000")
        {
            throw new FluentCMS.Services.InvalidParamException("1000");
        }
    });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.AfterQueryOne],
        (IDictionary<string,object> test) =>
        {
            test[TestEntity.FieldName] += "AfterQueryOne";
        });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.BeforeQueryMany], 
        (Filters _, Sorts sorts) =>
        {
            sorts.Add(new Sort
            {
                FieldName = TestEntity.FieldName,
                Order = SortOrder.Asc
            });
        });

    app.RegisterCmsHook(TestEntity.EntityName, [Occasion.AfterQueryMany], (ListResult result) =>
    {
        foreach (var item in result.Items)
        {
            item[TestEntity.FieldName] +=  " AfterQueryMany";
        }

    });
}