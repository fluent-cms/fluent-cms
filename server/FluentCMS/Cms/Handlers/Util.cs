using FluentCMS.Utils.ResultExt;
using Microsoft.AspNetCore.WebUtilities;

namespace FluentCMS.Cms.Handlers;

internal static class Util
{
    internal static StrArgs Args(this HttpContext context) =>
        QueryHelpers.ParseQuery(context.Request.QueryString.Value);

    internal static Task Html(this HttpContext context, string html, CancellationToken ct)
    {
        context.Response.ContentType = "text/html";
        return context.Response.WriteAsync(html, ct);
    }
    internal static T MustToEnum<T>(this string s)
    where T : struct, Enum
    {
        var v = s.ToEnum<T>();
        return v ?? throw new ResultException($"Cannot resolve '{typeof(T).Name}'");
    }
    internal static T? ToEnum<T>(this string s)
    where T : struct, Enum
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }
        var ret = Enum.TryParse<T>(s, true, out var result);
        return ret? result : throw new ResultException($"'{s}' is not a valid value for {typeof(T).Name}");
    }
}