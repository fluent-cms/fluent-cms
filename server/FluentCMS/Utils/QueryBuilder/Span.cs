using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.JsonElementExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Span(string? First = default, string? Last = default);

public sealed record ValidSpan(Span Span, ImmutableDictionary<string,object>? EdgeItem = default);

public static class SpanConstants
{
    public const string Cursor = "cursor";
    public const string HasPreviousPage = "hasPreviousPage";
    public const string HasNextPage = "hasNextPage";
    public const string SourceId = "sourceId";
}

public static class SpanHelper
{
    public static bool HasNext(IEnumerable<Record> items)
    {
        var last = items.LastOrDefault();
        return last is not null && last.TryGetValue(SpanConstants.HasNextPage, out var v) && v is true;
    }

    public static string LastCursor(IEnumerable<Record> items)
    {
        var val = items.Last().TryGetValue(SpanConstants.Cursor, out var v) ?v as string:"" ;
        return val ?? "";
    }

    public static string FirstCursor(IEnumerable<Record> items)
    {
        var val = items.First().TryGetValue(SpanConstants.Cursor, out var v) ? v as string : "";
        return val ?? "";
    }

    public static bool HasPrevious(IEnumerable<Record> items)
    {
        var first = items.FirstOrDefault();
        return first is not null && first.TryGetValue(SpanConstants.HasPreviousPage, out var v) && v is true;
    }
    

    public static object SourceId(this ValidSpan c) => c.EdgeItem![SpanConstants.SourceId];
    public static object Edge(this ValidSpan c, string fld) => c.EdgeItem![fld];
    
    public static bool IsEmpty(this Span c) => string.IsNullOrWhiteSpace(c.First) && string.IsNullOrWhiteSpace(c.Last);
    
    public static bool IsForward(this Span c) => !string.IsNullOrWhiteSpace(c.Last) || string.IsNullOrWhiteSpace(c.First);
    
    public static string GetCompareOperator(this Span c,string order)
    {
        return  c.IsForward() ? order == SortOrder.Asc ? ">" : "<":
            order == SortOrder.Asc ? "<" : ">";
    }

    public static Record[] ToPage(this Span c, Record[] items, int takeCount)
    {
        if (items.Length == 0)
        {
            return [];
        }

        var hasMore = items.Length > takeCount;
        if (hasMore)
        {
            items = items[..^1]; // remove last item
        }

        if (!c.IsForward())
        {
            items = [..items.Reverse()];
        }

        var (pre, next) = (hasMore, c.First??"", c.Last??"") switch
        {
            (true, "", "") => (false, true), // home page, should not has previous
            (false, "", "") => (false, false), // home page
            (true, _, _) => (true, true), // no matter click next or previous, show both
            (false, _, "") => (false, true), // click preview, should have next
            (false, "", _) => (true, false), // click next, should nave previous
            _ => (false, false)
        };
        items.First()[SpanConstants.HasPreviousPage] = pre;
        items.Last()[SpanConstants.HasNextPage] = next;
        return items;
    }

    public static void SetCursor(object? sourceId, Record item, IEnumerable<Sort> sorts)
    {
        var dict = new Dictionary<string, object>();
        foreach (var sort in sorts)
        {
            if (item.GetValueByPath<object>(sort.FieldName, out var val)) dict[sort.FieldName] = val!;
        }

        if (sourceId is not null)
        {
            dict[SpanConstants.SourceId] = sourceId;
        }
        var cursor = JsonSerializer.Serialize(dict);
        item[SpanConstants.Cursor] = Base64UrlEncoder.Encode(cursor);
    }
    
    public static Result<ValidSpan> ToValid(this Span c, IEnumerable<Attribute> attributes)
    {
        if (c.IsEmpty()) return new ValidSpan(c, default);

        var arr = attributes.ToArray();
        try
        {
            var recordStr = c.IsForward() ? c.Last : c.First;
            recordStr = Base64UrlEncoder.Decode(recordStr);
            var item = JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(recordStr);
            var dict = new Dictionary<string, object>();
            foreach (var (key, value) in item!)
            {
                var val = value.ToPrimitive();
                if (val is string s )
                {
                    var field = arr.FindOneAttr(key);
                    if (field is not null)
                    {
                        val = field.Cast(s);
                    }
                }

                dict[key] = val;
            }

            return new ValidSpan(c,dict.ToImmutableDictionary());
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
    }
}