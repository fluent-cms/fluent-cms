using System.Collections;
using System.Text.Json;
using FluentCMS.App;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using Swashbuckle.AspNetCore.SwaggerGen;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TestService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();
builder.AddKafkaMessageProducer("localhost:9092");
var app = builder.Build();
await app.UseCmsAsync(false);
app.RegisterMessageProducerHook();
var schemaService = app.GetCmsSchemaService();
var entity = await schemaService.GetEntityByNameOrDefault(TestEntity.EntityName);
if (entity.IsFailed)
{
    await schemaService.AddOrSaveSimpleEntity(TestEntity.EntityName, TestEntity.FieldName, "", "");
    var entityService = app.GetCmsEntityService();
    await entityService.Insert(TestEntity.EntityName, new Dictionary<string, object>
    {
        { TestEntity.FieldName, TestEntity.TestValue }
    });
}

RegisterHooks(app.GetCmsHookFactory());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();

return;
void RegisterHooks(HookRegistry registry)
{

    registry.AddHook(TestEntity.EntityName, Occasion.BeforeInsert, 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeInsert"; });
    registry.AddHook(TestEntity.EntityName, Occasion.AfterInsert, 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "AfterInsert"; });

    registry.AddHook(TestEntity.EntityName, Occasion.BeforeUpdate, 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeUpdate"; });

    registry.AddHook(TestEntity.EntityName, Occasion.AfterUpdate, 
        (IDictionary<string,object>test) => { test[TestEntity.FieldName] += "AfterUpdate"; });

    registry.AddHook(TestEntity.EntityName, Occasion.BeforeDelete, 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "BeforeDelete"; });

    registry.AddHook(TestEntity.EntityName, Occasion.AfterDelete, 
        (IDictionary<string,object> test) => { test[TestEntity.FieldName] += "AfterDelete"; });

    registry.AddHook(TestEntity.EntityName, Occasion.BeforeQueryOne, (RecordMeta meta) =>
    {
        if (meta.Id == "1000")
        {
            throw new FluentCMS.Services.InvalidParamException("1000");
        }
    });

    registry.AddHook(TestEntity.EntityName, Occasion.AfterQueryOne,
        (IDictionary<string,object> test) =>
        {
            test[TestEntity.FieldName] += "AfterQueryOne";
        });

    registry.AddHook(TestEntity.EntityName, Occasion.BeforeQueryMany, 
        (Filters _, Sorts sorts) =>
        {
            sorts.Add(new Sort
            {
                FieldName = TestEntity.FieldName,
                Order = SortOrder.Asc
            });
        });

    registry.AddHook(TestEntity.EntityName, Occasion.AfterQueryMany, (ListResult result) =>
    {
        foreach (var item in result.Items)
        {
            item[TestEntity.FieldName] +=  " AfterQueryMany";
        }

    });
}