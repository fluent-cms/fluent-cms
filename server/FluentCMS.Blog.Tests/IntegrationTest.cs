using System.Text;
using System.Text.Json;
using FluentCMS.Models;
using FluentCMS.Utils.HttpClientExt;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FluentCMS.IntegrationTests;

public class IntegrationTest
{
    private readonly HttpClient _client;
   
    public IntegrationTest()
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
        var teacher = await _client.GetObject<Dictionary<string, JsonElement>>("/api/entities/teacher/1");
        Assert.Equal("TomUpdate", teacher.Value["name"].GetString());
        
        await AddSimpleEntity("student", "name");
        await AddSimpleData("student", "name", "Bob");

        await AddSimpleEntity("class", "name", "teacher", "student");
        await AddSimpleData("class", new Dictionary<string, object>
        {
            { "name", "math" },
            { "teacher", 1 }
        });
        await SaveClassStudent();
        var response = await _client.GetAsync("/api/entities/class/1/student?exclude=false");
        response.EnsureSuccessStatusCode();
        response = await _client.GetAsync("/api/entities/class/1/student?exclude=true");
        response.EnsureSuccessStatusCode();

        var cls = await _client.GetObject<Dictionary<string,JsonElement>>("/api/entities/class/1");
        Assert.Equal(1, cls.Value["teacher_data"].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Query()
    {
        await Login();
        await AddSimpleEntity("student","name");
        for (var i = 0; i < 500; i++)
        {
            await AddSimpleData("student", "name", $"student{i}");
        }

        await AddSimpleData("student", "name", "good-student");
        await AddSimpleData("student", "name", "good-student");
        
        //single query
        
    }

    private async Task SaveClassStudent()
    {
        var item = new
        {
            id = 1
        };
        var payload = new object[] { item };
        var res = await _client.PostObject($"/api/entities/class/1/student/save", payload);
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
        var res = await _client.PostObject($"/api/entities/{entity}/update", payload);
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
        var res = await _client.PostObject($"/api/entities/{entity}/insert", payload);
        res.EnsureSuccessStatusCode();
    }

    private async Task AddSimpleEntity(string entity, string field)
    {
        await AddSimpleEntity(entity, field, "", "");
    }

    private async Task AddSimpleEntity(string entity, string field, string lookup, string crosstable)
    {
        var result =
            await _client.PostObject(
                $"/api/schemas/simple_entity_define?entity={entity}&field={field}&lookup={lookup}&crosstable={crosstable}",
                new Dictionary<string, object>());
        result.EnsureSuccessStatusCode();
    }

    private static StringContent Content(object payload) =>
        new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

    private async Task GetTopMenuBar()
    {
        var response = await _client.GetAsync("/api/schemas/top-menu-bar");
        response.EnsureSuccessStatusCode();
    }

    private async Task GetAllSchema()
    {
        var response = await _client.GetObject<Schema[]>("/api/schemas");
        Assert.True(response.Value.Length > 0);
    }

    private async Task Login()
    {
        // Arrange
        var loginData = new
        {
            email = "sadmin@cms.com",
            password = "Admin1!"
        };

        await _client.PostObject("/api/register", loginData);
        await _client.PostAndSaveCookie("/api/login?useCookies=true", loginData);
    }
}