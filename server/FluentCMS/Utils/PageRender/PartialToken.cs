using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace FluentCMS.Utils.PageRender;

public record PartialToken(
    string Page,
    string NodeId,
    string First,
    string Last,
    Repeat Repeat
    )
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