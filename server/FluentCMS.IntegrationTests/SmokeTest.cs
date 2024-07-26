using System.Net.Http.Json;
using System.Text;
using FluentCMS.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FluentCMS.IntegrationTests;

public class SmokeTest
{
    private readonly HttpClient _client;
    private readonly CookieHolder _cookieHolder = new ();
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
    public async Task TestHomePage()
    {
        var loginResponse = await _client.GetAsync("/");
        loginResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task Post_Login_ReturnsSuccess()
    {
        await Login();
    }

    [Fact]
    public async Task GetTopMenuBar()
    {
        await Login();
        var response = await GetWithCookie("/api/schemas/top-menu-bar");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAllSchema()
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

    private async Task<HttpResponseMessage> GetWithCookie(string uri) =>
       await _client.SendAsync(_cookieHolder.SetCookie(new HttpRequestMessage(HttpMethod.Get, uri)));
    private static StringContent Content(object payload) =>
        new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
}