using System.Net.Http.Json;
using System.Text;
using FluentCMS.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.IntegrationTests;

public class SmokeTest
{
    private readonly HttpClient _client;
    private readonly CookieHolder _cookieHolder = new();

    public SmokeTest()
    {
        var app = new WebApplicationFactory<Program>();
        _client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true
        });
    }

    [Fact]
    public async Task BasicFlow()
    {
        await Login();
        await GetTopMenuBar();
        await GetAllSchema();
        await AddSimpleEntity("teacher", "name");
        await AddSimpleData("teacher", "name", "Tom");
        await AddSimpleEntity("student", "name");
        await AddSimpleData("student", "name", "Bob");

        await AddSimpleEntity("class", "name", "teacher", "student");
        await AddSimpleData("class", new Dictionary<string, object>
        {
            { "name", "math" },
            { "teacher", 1 }
        });
        await SaveClassStudent();
        var response = await GetWithCookie("/api/entities/class/1/student?exclude=false");
        response.EnsureSuccessStatusCode();
        response = await GetWithCookie("/api/entities/class/1/student?exclude=true");
        response.EnsureSuccessStatusCode();
    }

   

    private async Task SaveClassStudent()
    {
        var item = new
        {
            id = 1
        };
        var payload = new object[] { item };
        var res = await PostWithCookie($"/api/entities/class/1/student/save",
            payload);
        res.EnsureSuccessStatusCode();

    }

    private async Task AddSimpleData(string entity, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { field, val }
        };
        await AddSimpleData(entity, payload);
    }

    private async Task AddSimpleData(string entity, Dictionary<string, object> payload)
    {
        var res = await PostWithCookie($"/api/entities/{entity}/insert", payload);
        res.EnsureSuccessStatusCode();
    }

    private async Task AddSimpleEntity(string entity, string field)
    {
        await AddSimpleEntity(entity, field, "", "");
    }

    private async Task AddSimpleEntity(string entity, string field, string lookup, string crossTable)
    {
        var schema = new Schema
        {
            Name = entity,
            Type = SchemaType.Entity,
            Settings = new Settings
            {
                Entity = new Entity
                {
                    Name = entity,
                    TableName = entity,
                    Title = entity,
                    DefaultPageSize = 10,
                    PrimaryKey = "id",
                    TitleAttribute = field,
                    Attributes =
                    [
                        new Attribute
                        {
                            Field = field,
                            Header = field,
                            InList = true,
                            InDetail = true,
                            DataType = DataType.String
                        }
                    ]
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(lookup))
        {
            schema.Settings.Entity.Attributes = schema.Settings.Entity.Attributes.Append(new Attribute
            {
                Field = lookup,
                Options = lookup,
                Header = lookup,
                InList = true,
                InDetail = true,
                DataType = DataType.Int,
                Type = DisplayType.lookup,
            }).ToArray();

        }

        if (!string.IsNullOrWhiteSpace(crossTable))
        {
            schema.Settings.Entity.Attributes = schema.Settings.Entity.Attributes.Append(new Attribute
            {
                Field = crossTable,
                Options = crossTable,
                Header = crossTable,
                DataType = DataType.Na,
                Type = DisplayType.crosstable,
                InDetail = true,
            }).ToArray();

        }

        var result = await PostWithCookie("/api/schemas/define", schema);
        result.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> PostWithCookie(string uri, object payload)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = Content(payload)
        };
        return await _client.SendAsync(_cookieHolder.SetCookie(message));
    }

    private async Task<HttpResponseMessage> GetWithCookie(string uri) =>
        await _client.SendAsync(_cookieHolder.SetCookie(new HttpRequestMessage(HttpMethod.Get, uri)));

    private static StringContent Content(object payload) =>
        new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

    private async Task GetTopMenuBar()
    {
        await Login();
        var response = await GetWithCookie("/api/schemas/top-menu-bar");
        response.EnsureSuccessStatusCode();
    }

    private async Task GetAllSchema()
    {
        await Login();
        var response = await GetWithCookie("/api/schemas");
        response.EnsureSuccessStatusCode();
        var schemas = await response.Content.ReadFromJsonAsync<Schema[]>();
        Assert.True(schemas?.Length > 0);
    }
     private async Task Login()
        {
            // Arrange
            var loginData = new
            {
                email = "admin@cms.com",
                password = "Admin1!"
            };
    
            var content = Content(loginData);
            await _client.PostAsync("/api/register", content);
            // Act
            var response = await _client.PostAsync("/api/login?useCookies=true", content);
            // Assert
            response.EnsureSuccessStatusCode();
            _cookieHolder.GetCookie(response);
        }
}