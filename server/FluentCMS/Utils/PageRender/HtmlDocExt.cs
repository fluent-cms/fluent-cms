using FluentResults;

namespace FluentCMS.Utils.PageRender;
using HtmlAgilityPack;

public static class HtmlDocExt
{
    public static Result<MultipleRecordNode[]> LoadMultipleRecordNode(this HtmlDocument doc)
    {
        var nodeCollection = doc.DocumentNode.SelectNodes(
            $"//section[@{Constants.DataSourceTypeTag}='{DataSourceType.MultipleRecords}']");
        if (nodeCollection is null)
        {
            return Result.Ok<MultipleRecordNode[]>([]);
        }

        var ret = new MultipleRecordNode[nodeCollection.Count];
        for (var i = 0; i < nodeCollection.Count; i++)
        {
            var node = nodeCollection[i];
            var queryRes = node.ParseMultipleRecordsQuery();
            if (queryRes.IsFailed)
            {
                return Result.Fail(queryRes.Errors);
            }

            var dict = node.GetFieldAttributes();

            ret[i] = new MultipleRecordNode(node.GetId(), node, node.OuterHtml, queryRes.Value,dict);
        }

        return ret;
    }

    private static Result<int> ParseInt32(this HtmlNode node, string attribute)
    {
        var s = node.GetAttributeValue(Attributes.Offset, string.Empty);
        if (s == string.Empty) return 0;
        if (!int.TryParse(s, out var i))
        {
            return Result.Fail($"Invalid int value of {attribute}");
        }

        return i;
    }

    private static string GetId(this HtmlNode node)
    {
        var s = node.GetAttributeValue(Attributes.Id, string.Empty);
        return s!;
    }

    private static IDictionary<string, string> GetFieldAttributes(this HtmlNode node)
    {
        return node.Attributes
            .Where(attr => attr.Name.StartsWith(Attributes.FieldPrefix))
            .Select(x => new { Name = x.Name.Substring(Attributes.FieldPrefix.Length), x.Value })
            .ToDictionary(x => x.Name, x => string.IsNullOrWhiteSpace(x.Value) ? x.Name : x.Value);
    }

    private static Result<MultipleRecordQuery> ParseMultipleRecordsQuery(this HtmlNode div)
    {
        var query = div.GetAttributeValue(Attributes.Query, string.Empty);
        if (query == string.Empty)
        {
            return Result.Fail("can not find query");
        }

        var offset = div.ParseInt32(Attributes.Offset);
        if (offset.IsFailed)
        {
            return Result.Fail(offset.Errors);
        }

        var limit = div.ParseInt32(Attributes.Limit);
        if (limit.IsFailed)
        {
            return Result.Fail(limit.Errors);
        }

        var qs = div.GetAttributeValue(Attributes.Qs, string.Empty);
        return new MultipleRecordQuery(query, qs, offset.Value, limit.Value);
    }
}