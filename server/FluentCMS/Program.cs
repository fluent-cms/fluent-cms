using FluentCMS.Services;
using FluentCMS.Data;
using FluentCMS.Utils.Dao;
using FluentCMS.Utils.File;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy  =>
        {
            policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173").
                AllowAnyHeader().AllowCredentials();
        });
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
}); 
builder.Services.AddControllers();
builder.Services.AddSingleton<FileUtl, FileUtl>(p => new FileUtl("wwwroot/files"));
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IEntityService, EntityService >();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("PgConnection");
if (conn is not null)
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conn));
    builder.Services.AddSingleton<IDao>(p => new PgDao(conn,builder.Environment.IsDevelopment()));

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

app.UseCors("AllowAllOrigins");
app.UseAuthorization();

app.MapControllers();

app.Run();