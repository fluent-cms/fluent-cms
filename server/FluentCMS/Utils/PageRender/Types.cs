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
}

public record MultipleRecordQuery(string Query, string? Qs, int Offset, int Limit);

public record MultipleRecordNode(HtmlNode HtmlNode,MultipleRecordQuery MultipleQuery);

public record MultipleRecordData(Record[] Items);


