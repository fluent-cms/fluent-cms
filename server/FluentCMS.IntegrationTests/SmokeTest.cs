using System.Net.Http.Json;
using System.Text;
using FluentCMS.Models;
using FluentCMS.Services;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;
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
        
        await UpdateSimpleData("teacher", 1, "name", "TomUpdate");
        var teacher = await GetObjectWithCookie("/api/entities/teacher/1");
        Assert.Equal("TomUpdate", (string)teacher.name);
        
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

        var cls = await GetObjectWithCookie("/api/entities/class/1");
        Assert.Equal(1, (int)cls.teacher_data.id);
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
    
    private async Task UpdateSimpleData(string entity,int id, string field, string val)
    {
        var payload = new Dictionary<string, object>
        {
            { "id", id },
            { field, val }
        };
        await UpdateSimpleData(entity, payload);
    }
    private async Task UpdateSimpleData(string entity, Dictionary<string, object> payload)
    {
        var res = await PostWithCookie($"/api/entities/{entity}/update", payload);
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

    private async Task AddSimpleEntity(string entity, string field, string lookup, string crosstable)
    {
        var result =
            await PostWithCookie(
                $"/api/schemas/simple_entity_define?entity={entity}&field={field}&lookup={lookup}&crosstable={crosstable}",
                new Dictionary<string, object>());
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

    private async Task<dynamic> GetObjectWithCookie(string uri)
    {
        var res = await GetWithCookie(uri);
        res.EnsureSuccessStatusCode();
        dynamic payload = JObject.Parse(await res.Content.ReadAsStringAsync());
        return payload;
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