using System.Text;
using FluentResults;
using System.Text.Json;

namespace FluentCMS.Utils.HttpClientExt;

public static class HttpClientExt
{
    public static async Task<Result<T>> GetResult<T>(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult<T>();
    }
    public static async Task<Result> GetResult(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult();
    }
    public static async Task<Result<T>> PostResult<T>(this HttpClient client, string url, object payload)
    {
        var res = await client.PostAsync(url, Content(payload));
        return await res.ParseResult<T>();
    }

    public static async Task<Result> PostResult( this HttpClient client, string url, object payload )
    {
        var res= await client.PostAsync(url, Content(payload));
        return await res.ParseResult();
    }

    public static async Task<Result> PostAndSaveCookie(this HttpClient client, string url, object payload)
    {
        var response = await client.PostAsync(url, Content(payload));
        client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie"));
        return await response.ParseResult();
    }
    
    public static async Task<Result> DeleteResult(this HttpClient client, string uri)
    {
        var res = await client.DeleteAsync(uri);
        return await res.ParseResult();
    }
    
    private static async Task<Result> ParseResult(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"fail to request {msg.RequestMessage?.RequestUri}, message= {str}");
        }
        return Result.Ok();
    }
    
    private static async Task<Result<T>> ParseResult<T>(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"fail to request {msg.RequestMessage?.RequestUri}, message= {str}");
        }

        var item = JsonSerializer.Deserialize<T>(str, CaseInsensitiveOption);
        return item is null ? Result.Fail("Fail to Deserialize") : item;
    }

    private static StringContent Content(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static JsonSerializerOptions CaseInsensitiveOption => new() { PropertyNameCaseInsensitive = true };
}