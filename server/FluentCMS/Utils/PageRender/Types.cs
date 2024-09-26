using System.Text;
using FluentResults;
using HtmlAgilityPack;

namespace FluentCMS.Utils.PageRender;

public static class Constants
{
    public const string DataSourceTypeTag = "data-source-type";
}

public static class DataSourceType
{
    public const string  MultipleRecords= "multiple-records";
}

public static class Attributes
{
    public const string Query = "query";
    public const string Offset = "offset";
    public const string Limit = "limit";
    public const string Qs = "qs";
    public const string Field = "field";
}

public record MultipleRecordQuery(string Query, string? Qs, int Offset, int Limit)
{
    public override string ToString()
    {
        return $"{Query}_{Sanitize(Qs)}_{Offset}_{Limit}";

        string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return "";
            }
            var ret = new StringBuilder();
            foreach (var c in s)
            {
                if (c >= '0' && c <= '9' || c >= 'a' && c <= 'z')
                {
                    ret.Append(c);
                }
                else
                {
                    ret.Append('_');
                }
            }

            return ret.ToString();
        }
    }
}

public record MultipleRecordNode(HtmlNode HtmlNode,string Field, Result<MultipleRecordQuery> MultipleQuery);


