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
    public async Task TestHooks()
    {
        var content = new Dictionary<string, object>
        {
            { TestEntity.FieldName, TestEntity.TestValue },
        };
        //insert
        var testEntity = await _client.PostResult<TestEntity>($"/api/entities/{TestEntity.EntityName}/insert", content);
        Assert.Equal(TestEntity.TestValue + "BeforeInsert" + "AfterInsert", testEntity.Value.TestName);
        
        content = new Dictionary<string, object>
        {
            { "id", testEntity.Value.id },
            { TestEntity.FieldName, TestEntity.TestValue },
        };
        
        //update
        testEntity = await _client.PostResult<TestEntity>($"/api/entities/{TestEntity.EntityName}/update", content);
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterUpdate", testEntity.Value.TestName);
        
        //query one 
        testEntity = await _client.GetResult<TestEntity>($"/api/entities/{TestEntity.EntityName}/{testEntity.Value.id}");
        Assert.Equal(TestEntity.TestValue + "BeforeUpdate" + "AfterQueryOne", testEntity.Value.TestName);
        
        var resMessage = await _client.GetAsync($"/api/entities/{TestEntity.EntityName}/1000");
        Assert.False(resMessage.IsSuccessStatusCode);
        
        //delete
        testEntity = await _client.PostResult<TestEntity>($"/api/entities/{TestEntity.EntityName}/delete", content);
        Assert.Equal(TestEntity.TestValue + "BeforeDelete" + "AfterDelete", testEntity.Value.TestName);

        //query many
        await _client.PostResult<TestEntity>($"/api/entities/{TestEntity.EntityName}/insert", content);
        var listResult = await _client.GetFromJsonAsync<ListResponse>($"/api/entities/{TestEntity.EntityName}");
        Assert.NotNull(listResult);

        var last = listResult.Items.Last();
        Assert.EndsWith("AfterQueryMany", last[TestEntity.FieldName].ToString());
    }
}