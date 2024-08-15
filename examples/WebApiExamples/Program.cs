using FluentCMS.App;
using FluentCMS.Utils.HookFactory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSqliteCms("Data Source=cms1.db").PrintVersion();
//builder.AddKafkaMessageProducer("localhost:9092");    
var app = builder.Build();
await app.UseCmsAsync(false);
var schemaService = app.GetCmsSchemaService();
await schemaService.AddOrSaveSimpleEntity("student", "Name", null, null);
await schemaService.AddOrSaveSimpleEntity("teacher", "Name", null, null);
await schemaService.AddOrSaveSimpleEntity("class", "Name", "teacher", "student");

var hooks = app.GetCmsHookFactory();
hooks.AddHook("teacher", Occasion.BeforeInsert,(IDictionary<string,object> payload) =>
{
    payload["Name"] = "Teacher " + payload["Name"];
});
//app.RegisterMessageProducerHook();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.Run();
