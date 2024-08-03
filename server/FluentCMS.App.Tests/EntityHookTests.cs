using System.Net.Http.Json;
using FluentCMS.Utils.HttpClientExt;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.AspNetCore.Mvc.Testing;

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
        var testEntity = await _client.PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/insert", content);
        Assert.Equal(TestEntity.TestValue + "BeforeInsert" + "AfterInsert", testEntity.Value.TestName);
        
        content = new Dictionary<string, object>
        {
            { "id", testEntity.Value.id },
            { TestEntity.FieldName, TestEntity.TestValue },
        };
        
        testEntity = await _client.PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/update", content);
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterUpdate", testEntity.Value.TestName);
        
        
        testEntity = await _client.GetObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/{testEntity.Value.id}");
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterQueryOne", testEntity.Value.TestName);
        
        var resMessage = await _client.GetAsync($"/api/entities/{TestEntity.EntityName}/1000");
        Assert.False(resMessage.IsSuccessStatusCode);
        
        testEntity = await _client.PostObject<TestEntity>($"/api/entities/{TestEntity.EntityName}/delete", content);
        Assert.Equal(TestEntity.TestValue + "BeforeDelete" + "AfterDelete", testEntity.Value.TestName);

        content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue + "BeforeQueryMany-2" }
        };
        await _client.PostObject($"/api/entities/{TestEntity.EntityName}/insert", content);
        content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue + "BeforeQueryMany-1" }
        };
        await _client.PostObject($"/api/entities/{TestEntity.EntityName}/insert", content);

        var listResult = await _client.GetFromJsonAsync<ListResult>($"/api/entities/{TestEntity.EntityName}");
        Assert.NotNull(listResult);
        var first = listResult.Items.First();
        Assert.Equal(TestEntity.TestValue + "BeforeQueryMany-1" + "BeforeInsert",first[TestEntity.FieldName].ToString());
        var last = listResult.Items.Last();
        Assert.Equal("AfterQueryMany", last[TestEntity.FieldName].ToString());
    }
}