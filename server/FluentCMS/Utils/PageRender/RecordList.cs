using FluentCMS.Utils.RecordExt;
using HtmlAgilityPack;

namespace FluentCMS.Utils.PageRender;

public static class Constants
{
    public const string DataSourceTypeTag = "data-source-type";
}

public static class DataSourceType
{
    public const string  MultipleRecords= "multiple-records";
    public const string SingleRecord = "signle-record";
    public const string PagedRecords = "paged-record";
}

public static class Attributes
{
    public const string Id = "id";
    public const string Query = "query";
    public const string Offset = "offset";
    public const string Limit = "limit";
    public const string Qs = "qs";
    public const string FieldPrefix = "field-";
}


public record MultipleRecordQuery(string Query, string? Qs, int Offset, int Limit);

public record MultipleRecordNode(string Id, HtmlNode HtmlNode,string Html, MultipleRecordQuery MultipleQuery, IDictionary<string,string> Mapping);

public record MultipleRecordData(Record[] Items)
{
    public MultipleRecordData Map(IDictionary<string, string> mapping)
    {
        return new MultipleRecordData(Items.Select(MapRecord).ToArray());
        
        Record MapRecord(Record item)
        {
             var dict = new Dictionary<string, object>();
             foreach (var (to, from) in mapping)
             {
                 dict[to] = item.Value(from);
             }
             return dict;
        }
    }
}

