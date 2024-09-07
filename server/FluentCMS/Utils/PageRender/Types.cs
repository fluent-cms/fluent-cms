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
    public const string SingleRecord = "single-record";
    public const string PagedRecords = "paged-record";
}

public static class Attributes
{
    public const string Id = "id";
    public const string Query = "query";
    public const string Offset = "offset";
    public const string Limit = "limit";
    public const string Qs = "qs";
    public const string Field = "field";
}

public record MultipleRecordQuery(string Query, string? Qs, int Offset, int Limit);

public record MultipleRecordNode(string Id, HtmlNode HtmlNode,string Field, Result<MultipleRecordQuery> MultipleQuery);


