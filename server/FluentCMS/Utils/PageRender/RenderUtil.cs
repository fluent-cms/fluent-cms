using FluentResults;
using HandlebarsDotNet;

namespace FluentCMS.Utils.PageRender;
using HtmlAgilityPack;

public static class RenderUtil
{
    public static void SetLoopAndPagination(this HtmlNode node, string field, PaginationType paginationType)
    {
        node.AddLoop(field);
        node.AddCursor(field);
        if (paginationType == PaginationType.InfiniteScroll)
        {
            node.AddPagination(field);
        }
    }

    public static void SetLoopAndPagination(this IEnumerable<RepeatNode> repeatNodes)
    {
        foreach (var repeatNode in repeatNodes)
        {
            repeatNode.HtmlNode.SetLoopAndPagination(repeatNode.Repeat.Field, repeatNode.Repeat.PaginationType);
        }
    }

    public static Result<RepeatNode[]> GetRepeatingNodes(this HtmlDocument doc)
    {
        var nodeCollection =
            doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataSourceType}='{Constants.MultipleRecords}']");

        if (nodeCollection is null)
        {
            return Result.Ok<RepeatNode[]>([]);
        }

        var ret = new List<RepeatNode>();
        foreach (var n in nodeCollection)
        {
            var query = n.GetAttributeValue(Constants.AttrQuery, string.Empty);
            var qs = n.GetAttributeValue(Constants.AttrQs, string.Empty);

            var (_, _, offset, offsetErr) = n.ParseInt32(Constants.AttrOffset);
            if (offsetErr is not null)
            {
                return Result.Fail(offsetErr);
            }

            var (_, _, limit, limitErr) = n.ParseInt32(Constants.AttrLimit);
            if (limitErr is not null)
            {
                return Result.Fail(limitErr);
            }

            var field = n.GetAttributeValue(Constants.AttrField, string.Empty);
            if (string.IsNullOrWhiteSpace(field))
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Result.Fail(
                        $"both field and query was not set for multiple-record element [{n.OuterHtml}]");
                }

                field = n.Id;
            }

            var pagination = n.GetAttributeValue(Constants.AttrPagination, PaginationType.None.ToString());
            if (!Enum.TryParse(pagination, out PaginationType paginationType))
            {

                return Result.Fail(
                    $"both field and query was not set for multiple-record element [{n.OuterHtml}]");
            }

            ret.Add(new RepeatNode(n, new Repeat(paginationType, field, query, qs, offset, limit)));
        }

        return ret.ToArray();
    }

    private static Result<int> ParseInt32(this HtmlNode node, string attribute)
    {
        var s = node.GetAttributeValue(attribute, string.Empty);
        if (s == string.Empty) return 0;
        if (!int.TryParse(s, out var i))
        {
            return Result.Fail($"Invalid int value of {attribute}");
        }

        return i;
    }

    public static string RemoveBrace(string fullRouterParamName) => fullRouterParamName[1..^1];

    public static string GetBody(HtmlDocument doc, Record data)
    {
        var html = doc.DocumentNode.FirstChild.InnerHtml;
        var template = Handlebars.Compile(html);
        return template(data);
    }

    public static string GetTitle(string title, Record data) => Handlebars.Compile(title)(data);

    private static void AddPagination(this HtmlNode node, string field)
    {
        node.InnerHtml +=
            $"<div class=\"load-more-trigger\" style=\"visibility:hidden;\" last=\"{{{{{field}_last}}}}\"></div>";
    }

    private static void AddLoop(this HtmlNode node, string field)
    {
        node.InnerHtml = "{{#each " + field + "}}" + node.InnerHtml + "{{/each}}";
    }

    private static void AddCursor(this HtmlNode node, string field)
    {
        node.Attributes.Add("first", $"{{{{{FirstAttributeTag(field)}}}}}");
        node.Attributes.Add("last", $"{{{{{LastAttributeTag(field)}}}}}");
    }

    public static string FirstAttributeTag(string field) => $"{field}_first";
    public static string LastAttributeTag(string field) => $"{field}_last";
    //value of b overwrite a
    public static Dictionary<TK, TV> MergeDict<TK, TV>(Dictionary<TK, TV> a, Dictionary<TK, TV> b)
        where TK : notnull
    {
        var ret = new Dictionary<TK, TV>(a);
        foreach (var (k, v) in b)
        {
            ret[k] = v;
        }

        return ret;
    }
}