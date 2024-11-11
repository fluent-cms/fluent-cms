using System.Text;
using FluentResults;
using System.Text.Json;

namespace FluentCMS.Utils.HttpClientExt;

public static class HttpClientExt
{
    public static async Task<Result> ToResult(this HttpResponseMessage msg)
    {
        return msg.IsSuccessStatusCode
            ? Result.Ok()
            : Result.Fail($"fail to request {msg.RequestMessage?.RequestUri}, message= {await msg.Content.ReadAsStringAsync()}");
    }

    private static async Task<Result<T>> ToResult<T>(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"fail to request {msg.RequestMessage?.RequestUri}, message= {await msg.Content.ReadAsStringAsync()}");
        }
        var item = JsonSerializer.Deserialize<T>(str, CaseInsensitiveOption);
        return item is null ? Result.Fail("Fail to Deserialize") : item;
    }

    private static  JsonSerializerOptions CaseInsensitiveOption => new() { PropertyNameCaseInsensitive = true };
    public static async Task<Result<T>> GetObject<T>(this HttpClient client,string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ToResult<T>();
    }
    
    public static async Task<Result<T>> PostObject<T>(this HttpClient client, string url, object payload)
    {
        var res = await client.PostAsync(url, Content(payload));
        return await res.ToResult<T>();

    }

    public static async Task<HttpResponseMessage> PostObject(this HttpClient client, string url, object payload)
    {
        return await client.PostAsync(url, Content(payload));
    }

    public static async Task<HttpResponseMessage> PostAndSaveCookie(this HttpClient client, string url, object payload)
    {
        var response = await client.PostAsync(url, Content(payload));
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie"));
        return response;
    }
    
    private static StringContent Content(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    
    
}