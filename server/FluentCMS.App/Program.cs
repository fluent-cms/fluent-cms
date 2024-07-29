using FluentCMS.App;
using FluentCMS.Utils.HookFactory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TestService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var server = builder.CreateSqliteAppBuilder("Data Source=cms.db");
server.PrintVersion();

server.RegisterHook("test", Occasion.AfterQueryOne, Next.Continue,
    (IDictionary<string, object> record, TestService service) => service.HandleRecord(record));


var app = builder.Build();
await app.UseFluentCmsAsync(false);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();