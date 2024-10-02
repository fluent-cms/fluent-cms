using FluentResults;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using static System.Enum;

namespace FluentCMS.Utils.PageRender;
using HtmlAgilityPack;

public static class HtmlDocExt
{
    public static void AddPagination(this HtmlNode node, string field)
    {
        node.InnerHtml += $"<div class=\"load-more-trigger\" style=\"visibility:hidden;\" last=\"{{{{{field}.last}}}}\"></div>";
    }

    public static void AddLoop(this HtmlNode node, string field)
    {
        node.InnerHtml = "{{#each " + field + "}}" + node.InnerHtml + "{{/each}}";
    }

    public static void AddCursor(this HtmlNode node, string field)
    {
        node.Attributes.Add("first", $"{{{{{field}.first}}}}");
        node.Attributes.Add("last", $"{{{{{field}.last}}}}");
    }


    public static void SetLoopAndPagination(this RepeatNode[] repeatNodes)
    {
        foreach (var repeatNode in repeatNodes)
        {
            repeatNode.HtmlNode.AddLoop(repeatNode.MultipleQuery is null ? repeatNode.Field :repeatNode.Field+ ".items");
            repeatNode.HtmlNode.AddCursor(repeatNode.Field);
            if (repeatNode.PaginationType == PaginationType.InfiniteScroll)
            {
                repeatNode.HtmlNode.AddPagination(repeatNode.Field);
            }
        }
    }
  
    public static Result<RepeatNode[]> GetRepeatingNodes(this HtmlDocument doc, Dictionary<string,StringValues> baseDict)
    {
        var nodeCollection =
            doc.DocumentNode.SelectNodes($"//*[@{Constants.AttrDataSourceType}='{Constants.MultipleRecords}']");
        
        if (nodeCollection is null)
        {
            return Result.Ok<RepeatNode[]>([]);
        }

        var ret = new List<RepeatNode>();
        foreach (var htmlNode in nodeCollection)
        {
            var query = htmlNode.ParseMultiQuery(baseDict);
            if (query.IsFailed)
            {
                return Result.Fail(query.Errors);
            }
            
            var field = htmlNode.GetAttributeValue(Constants.AttrField, string.Empty);
            if (string.IsNullOrWhiteSpace(field) )
            {
                if (query.Value is null)
                {
                    return Result.Fail($"both field and query was not set for multiple-record element [{htmlNode.OuterHtml}]");
                }
                field = htmlNode.Id;
            }
            var pagination = htmlNode.GetAttributeValue(Constants.AttrPagination, PaginationType.None.ToString());
            if (!TryParse(pagination, out PaginationType p))
            {
                continue;
            }
            ret.Add(new RepeatNode(htmlNode, field, p, query.Value ));
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


    private static Result<MultiQuery?> ParseMultiQuery(this HtmlNode div, Dictionary<string,StringValues> baseDict)
    {
        var query = div.GetAttributeValue(Constants.AttrQuery, string.Empty);
        if (query == string.Empty)
        {
            return Result.Ok();
        }

        var offset = div.ParseInt32(Constants.AttrOffset);
        if (offset.IsFailed)
        {
            return Result.Fail(offset.Errors);
        }

        var limit = div.ParseInt32(Constants.AttrLimit);
        if (limit.IsFailed)
        {
            return Result.Fail(limit.Errors);
        }

        var qs = div.GetAttributeValue(Constants.AttrQs, string.Empty);
        var dict = MergeDict(baseDict, QueryHelpers.ParseQuery(qs));
        return new MultiQuery(query, dict, offset.Value, limit.Value);
    }
    
    //value of b overwrite a
    private static Dictionary<TK, TV> MergeDict<TK,TV>(Dictionary<TK, TV> a, Dictionary<TK, TV> b) where TK : notnull
    {
        var ret = new Dictionary<TK, TV>(a);
        foreach (var (k,v) in b)
        {
            ret[k] = v;
        }

        return ret;
    }
}