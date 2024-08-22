using System.Text.RegularExpressions;
using FluentCMS.Services;
using FluentCMS.Utils.HookFactory;
using FluentCMS.WebAppExt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSqliteCms("Data Source=cms.db");
var app = builder.Build();
await app.UseCmsAsync();

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
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.Run();
