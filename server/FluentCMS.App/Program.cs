using FluentCMS.App;
using FluentCMS.Cms.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.Nosql;
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

var mongoConfig = builder.Configuration.GetRequiredSection("MongoConfig").Get<MongoConfig>()!;
builder.AddSqliteCms("Data Source=cms.db");
builder.AddKafkaMessageProducer("localhost:9092");
builder.AddMongoView(mongoConfig);

var app = builder.Build();
await app.UseCmsAsync();

//app.RegisterMessageProducerHook("post");
//app.RegisterMongoViewHook("mongo-posts");

using var scope = app.Services.CreateScope();

var schemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
var entity = await schemaService.GetLoadedEntity(TestEntity.EntityName);
if (entity.IsFailed)
{
    await schemaService.EnsureSimpleEntity(TestEntity.EntityName, TestEntity.FieldName, "", "");
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
    var registry = app.GetHookRegistry();

    registry.EntityPreAdd.Register(TestEntity.EntityName, 
        args => { 
            args.RefRecord[TestEntity.FieldName] += "BeforeInsert";
            return args;
        });
    
    registry.EntityPostAdd.Register(TestEntity.EntityName,
        args => { 
            args.Record[TestEntity.FieldName] += "AfterInsert";
            return args;
        });
    
    registry.EntityPreUpdate.Register(TestEntity.EntityName,
        args =>
        {
            args.RefRecord[TestEntity.FieldName] += "BeforeUpdate";
            return args;
        });
    
    registry.EntityPostUpdate.Register(TestEntity.EntityName,
        args =>
        {
            args.Record[TestEntity.FieldName] += "AfterUpdate"; 
            return args;
        });

    registry.EntityPreDel.Register(TestEntity.EntityName,
        param =>
        {
            param.RefRecord[TestEntity.FieldName] += "BeforeDelete";
            return param;
        });

    registry.EntityPostDel.Register(TestEntity.EntityName,
        param =>
        {
            param.Record[TestEntity.FieldName] += "AfterDelete";
            return param;
        });

    registry.EntityPreGetOne.Register(TestEntity.EntityName,
        (param )=>
        {
            if (param.RecordId == "1000")
            {
                throw new FluentCMS.Services.InvalidParamException("1000");
            }
            return param;
        });

    registry.EntityPostGetOne.Register(TestEntity.EntityName,
        param =>
        {
            param.Record[TestEntity.FieldName] += "AfterQueryOne";
            return param;
        });
    registry.EntityPreGetList.Register(TestEntity.EntityName,
        param => param with { RefSorts = [..param.RefSorts, new Sort(TestEntity.FieldName, SortOrder.Asc)] });

    registry.EntityPostGetList.Register(TestEntity.EntityName, (param) =>
    {
        foreach (var item in param.RefListResult.Items)
        {
            item[TestEntity.FieldName] +=  " AfterQueryMany";
        }
        return param;
    });
}