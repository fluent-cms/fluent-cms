using FluentResults;

namespace FluentCMS.Utils.PageRender;
using HtmlAgilityPack;

public static class HtmlDocExt
{
    public static Result<MultipleRecordNode[]> LoadMultipleRecordNode(this HtmlDocument doc)
    {
        var nodeCollection =
            doc.DocumentNode.SelectNodes($"//*[@{Constants.DataSourceTypeTag}='{DataSourceType.MultipleRecords}']");
        
        if (nodeCollection is null)
        {
            return Result.Ok<MultipleRecordNode[]>([]);
        }

        var ret = new List<MultipleRecordNode>();
        foreach (var htmlNode in nodeCollection)
        {
            var field = htmlNode.GetAttributeValue(Attributes.Field, string.Empty);
            if (string.IsNullOrWhiteSpace(field))
            {
                return Result.Fail("can not find field of a multiple records name");
            }
            ret.Add(new MultipleRecordNode(htmlNode.GetId(), htmlNode, field, htmlNode.ParseMultipleRecordsQuery()));
        }
        return ret.ToArray();
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