using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace FluentCMS.Utils.PageRender;

public enum PaginationType
{
    None,
    Button,
    InfiniteScroll
}

public record RepeatNode(HtmlNode HtmlNode, string Field, PaginationType PaginationType, MultiQuery MultipleQuery);

public record PartialToken(
    string Page,
    string NodeId,
    string Field,
    PaginationType PaginationType,
    string Query,
    int Offset,
    int Limit,
    string First,
    string Last,
    Dictionary<string, StringValues> Qs)
{
    public override string ToString()
    {
        var cursor = JsonSerializer.Serialize(this);
        return Base64UrlEncoder.Encode(cursor);
    }

    public static PartialToken? Parse(string s)
    {
        s = Base64UrlEncoder.Decode(s);
        return JsonSerializer.Deserialize<PartialToken>(s);
    }
}