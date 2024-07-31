using FluentCMS.App;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TestService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.AddSqliteCms("Data Source=cmsapp.db").PrintVersion();

var app = builder.Build();

await app.UseCmsAsync(false);

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
void RegisterHooks(HookFactory factory)
{

    factory.AddHook(TestEntity.EntityName, Occasion.BeforeInsert, Next.Continue,
        (TestEntity test) => { test.TestName += "BeforeInsert"; });
    factory.AddHook(TestEntity.EntityName, Occasion.AfterInsert, Next.Continue,
        (TestEntity test) => { test.TestName += "AfterInsert"; });

    factory.AddHook(TestEntity.EntityName, Occasion.BeforeUpdate, Next.Continue,
        (TestEntity test) => { test.TestName += "BeforeUpdate"; });

    factory.AddHook(TestEntity.EntityName, Occasion.AfterUpdate, Next.Continue,
        (TestEntity test) => { test.TestName += "AfterUpdate"; });

    factory.AddHook(TestEntity.EntityName, Occasion.BeforeDelete, Next.Continue,
        (TestEntity test) => { test.TestName += "BeforeDelete"; });

    factory.AddHook(TestEntity.EntityName, Occasion.AfterDelete, Next.Continue,
        (TestEntity test) => { test.TestName += "AfterDelete"; });

    factory.AddHook(TestEntity.EntityName, Occasion.BeforeQueryOne, Next.Continue, (string id) =>
    {
        if (id == "1000")
        {
            throw new FluentCMS.Services.InvalidParamException("1000");
        }
    });

    factory.AddHook(TestEntity.EntityName, Occasion.AfterQueryOne, Next.Continue,
        (TestEntity test) => { test.TestName += "AfterQueryOne"; });

    factory.AddHook(TestEntity.EntityName, Occasion.BeforeQueryMany, Next.Continue,
        (Filters filters, Sorts sorts) =>
        {
            filters.Add(new Filter
            {
                FieldName = TestEntity.FieldName,
                Constraints =
                [
                    new Constraint
                    {
                        Match = Matches.Contains,
                        Value = "BeforeQueryMany"
                    }
                ],
            });
            sorts.Add(new Sort
            {
                FieldName = TestEntity.FieldName,
                Order = SortOrder.Asc
            });
        });

    factory.AddHook(TestEntity.EntityName, Occasion.AfterQueryMany, Next.Continue, (ListResult result) =>
    {
        result.Items.Add(new Dictionary<string, object>
        {
            { TestEntity.FieldName, "AfterQueryMany" }
        });
    });
}