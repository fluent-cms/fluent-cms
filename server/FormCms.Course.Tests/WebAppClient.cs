using Microsoft.AspNetCore.Mvc.Testing;
namespace FormCMS.Course.Tests;

public class WebAppClient<T>
where T :class
{
    private readonly HttpClient _httpClient;

    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    public WebAppClient()
    {
        var app = new WebApplicationFactory<T>();
        _httpClient = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            HandleCookies = true
        });
    }
}