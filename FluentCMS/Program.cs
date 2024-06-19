using FluentCMS.Services;
using FluentCMS.Data;
using FluentCMS.Utils;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
}); 
builder.Services.AddControllers();

builder.Services.AddScoped<ISchemaService, SchemaService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("PgConnection");
if (conn is not null)
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conn));
    builder.Services.AddScoped<IDao>(p => new PgDao(conn));

}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();