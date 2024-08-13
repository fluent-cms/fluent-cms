using System.Text;
using FluentResults;
using System.Text.Json;

namespace FluentCMS.Utils.HttpClientExt;

public static class HttpClientExt
{
    private static  JsonSerializerOptions CaseInsensitiveOption => new() { PropertyNameCaseInsensitive = true };
    public static async Task<Result<T>> GetObject<T>(this HttpClient client,string uri)
    {
        var res = await client.GetAsync(uri);
        if (!res.IsSuccessStatusCode)
        {
            return Result.Fail($"Fail to request ${uri}");
        }
        var str = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(str, CaseInsensitiveOption) ?? Result.Fail<T>("failed to parse result");
    }
    
    public static async Task<Result<T>> PostObject<T>(this HttpClient client, string url, object payload)
    {
        var res = await client.PostAsync(url, Content(payload));
        var str = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(str,CaseInsensitiveOption) ?? Result.Fail<T>("failed to parse result");
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