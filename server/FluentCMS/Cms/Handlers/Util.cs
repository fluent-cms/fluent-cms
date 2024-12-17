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
}