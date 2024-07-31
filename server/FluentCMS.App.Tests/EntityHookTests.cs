using System.Net.Http.Json;
using System.Text;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentCMS.App.Tests;



public class EntityHookTests
{
    
    private readonly HttpClient _client;

    public EntityHookTests()
    {
        var app = new WebApplicationFactory<Program>();
        _client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true
        }); 
    }

    [Fact]
    public async void TestHooks()
    {
        var content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue },
        };
        var testEntity = await PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/insert", content);
        Assert.Equal(TestEntity.TestValue + "BeforeInsert" + "AfterInsert", testEntity.TestName);
        
        content = new Dictionary<string, object>
        {
            { "id", testEntity.id },
            { TestEntity.FieldName, TestEntity.TestValue },
        };
        testEntity = await PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/update", content);
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterUpdate", testEntity.TestName);
        
        
        testEntity = await GetObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/{testEntity.id}");
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterQueryOne", testEntity.TestName);
        
        var resMessage = await _client.GetAsync($"/api/entities/{TestEntity.EntityName}/1000");
        Assert.False(resMessage.IsSuccessStatusCode);
        
        testEntity = await PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/delete", content);
        Assert.Equal(TestEntity.TestValue + "BeforeDelete" + "AfterDelete", testEntity.TestName);

        content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue + "BeforeQueryMany-2" }
        };
        await PostObject($"/api/entities/{TestEntity.EntityName}/insert", content);
        content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue + "BeforeQueryMany-1" }
        };
        await PostObject($"/api/entities/{TestEntity.EntityName}/insert", content);

        var listResult = await _client.GetFromJsonAsync<ListResult>($"/api/entities/{TestEntity.EntityName}");
        Assert.NotNull(listResult);
        var first = listResult.Items.First();
        Assert.Equal(TestEntity.TestValue + "BeforeQueryMany-1" + "BeforeInsert",first[TestEntity.FieldName].ToString());
        var last = listResult.Items.Last();
        Assert.Equal("AfterQueryMany", last[TestEntity.FieldName].ToString());
    }

    private async Task<T> PostObject<T>(string uri, IDictionary<string, object> val)
    {
        var res = await _client.PostAsync(uri, Content(val));
        res.EnsureSuccessStatusCode();
        var str = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(str)?? throw new Exception("failed to parse result");
    }

    private async Task<dynamic> PostObject(string uri, IDictionary<string,object> val)
    {
        var res = await _client.PostAsync(uri, Content(val));
        res.EnsureSuccessStatusCode();
        var str = await res.Content.ReadAsStringAsync();
        dynamic payload = JObject.Parse(str);
        return payload;
    }

    private async Task<T> GetObject<T>(string uri)
    {
        var res = await _client.GetAsync(uri);
        res.EnsureSuccessStatusCode();

        var str = await res.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(str) ?? throw new Exception("failed to parse result");
    }

    private async Task<dynamic> GetObject(string uri)
    {
        var res = await _client.GetAsync(uri);
        res.EnsureSuccessStatusCode();
        dynamic payload = JObject.Parse(await res.Content.ReadAsStringAsync());
        return payload;
    }
    private static StringContent Content(object payload) =>
        new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
}