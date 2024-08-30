using System.Text.RegularExpressions;
using FluentCMS.Auth.Services;
using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.WebAppExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApiExamples;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = "Data Source=cms.db";
builder.AddSqliteCms(connectionString);

//add fluent cms' permission control service 
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();

var app = builder.Build();

//use fluent cms' CRUD 
await app.UseCmsAsync();

//user fluent permission control feature
app.UseCmsAuth<IdentityUser>();
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [Roles.Sa]));
InvalidParamExceptionFactory.CheckResult(await app.EnsureCmsUser("admin@cms.com", "Admin1!", [Roles.Admin]));

app.RegisterCmsHook("teacher", [Occasion.BeforeInsert, Occasion.BeforeUpdate], VerifyTeacher);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.Run();

/////

void  VerifyTeacher(IDictionary<string,object> teacher) 
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

    return;

    bool IsValidEmail()
    {
        // Define a regex pattern for validating email addresses
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Return true if the email matches the pattern, otherwise false
        return regex.IsMatch(email);
    }
    bool IsValidPhoneNumber()
    {
        // Define a regex pattern for validating phone numbers
        string pattern = @"^\d{10}$|^\d{3}-\d{3}-\d{4}$";
        Regex regex = new Regex(pattern);

        // Return true if the phone number matches the pattern, otherwise false
        return regex.IsMatch(phoneNumber);
    }
}