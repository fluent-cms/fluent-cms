using System.Text;
using FluentResults;
using System.Text.Json;

namespace FormCMS.Utils.HttpClientExt;

public static class HttpClientExt
{
    public static async Task<Result<string>> GetStringResult(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseString();
    }

    public static async Task<Result<T>> GetResult<T>(this HttpClient client, string uri, JsonSerializerOptions? options)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult<T>(options);
    }
    public static async Task<Result> GetResult(this HttpClient client, string uri)
    {
        var res = await client.GetAsync(uri);
        return await res.ParseResult();
    }
    public static async Task<Result<T>> PostResult<T>(this HttpClient client, string url, object payload, JsonSerializerOptions? options)
    {
        var res = await client.PostAsync(url, Content(payload,options));
        return await res.ParseResult<T>(options);
    }

    public static async Task<Result> PostResult( this HttpClient client, string url, object payload, JsonSerializerOptions? options )
    {
        var res= await client.PostAsync(url, Content(payload,options));
        return await res.ParseResult();
    }

    public static async Task<Result> PostAndSaveCookie(this HttpClient client, string url, object payload,JsonSerializerOptions? options)
    {
        var response = await client.PostAsync(url, Content(payload,options));
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

    private static async Task<Result<string>> ParseString(this HttpResponseMessage msg)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"Fail to {msg.RequestMessage?.Method} {msg.RequestMessage?.RequestUri}, message= {str}");
        }
        return Result.Ok(str);
    }

    private static async Task<Result<T>> ParseResult<T>(this HttpResponseMessage msg, JsonSerializerOptions? options)
    {
        var str = await msg.Content.ReadAsStringAsync();
        if (!msg.IsSuccessStatusCode)
        {
            return Result.Fail(
                $"fail to request {msg.RequestMessage?.RequestUri}, message= {str}");
        }

        var item = JsonSerializer.Deserialize<T>(str,options);
        return item is null ? Result.Fail("Fail to Deserialize") : item;
    }

    private static StringContent Content(object payload, JsonSerializerOptions?options) =>
        new(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

}