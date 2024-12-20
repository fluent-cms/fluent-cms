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
    public static bool HasNext(Record item) 
        => item.TryGetValue(SpanConstants.HasNextPage, out var v) && v is true;
    public static bool HasNext(IEnumerable<Record> items) 
        => items.LastOrDefault() is { } last && HasNext(last);

    public static string LastCursor(Record item) =>
        item.TryGetValue(SpanConstants.Cursor, out var v) && v is string s ? s : null ?? "";

    public static string LastCursor(IEnumerable<Record> items)
        => LastCursor(items.Last());

    public static string FirstCursor(IEnumerable<Record> items)
        => items.First().TryGetValue(SpanConstants.Cursor, out var v) && v is string s? s: null ?? "";

    public static bool HasPrevious(Record item)
        => item.TryGetValue(SpanConstants.HasPreviousPage, out var v) && v is true;
    
    public static bool HasPrevious(IEnumerable<Record> items)
        => items.FirstOrDefault() is {} first && HasPrevious(first);

    public static ValidValue SourceId(this ValidSpan c) => c.EdgeItem![SpanConstants.SourceId].ToValidValue();
    public static ValidValue Edge(this ValidSpan c, string fld) => c.EdgeItem![fld].ToValidValue();
    
    public static bool IsEmpty(this Span c) => string.IsNullOrWhiteSpace(c.First) && string.IsNullOrWhiteSpace(c.Last);
    
    public static bool IsForward(this Span c) => !string.IsNullOrWhiteSpace(c.Last) || string.IsNullOrWhiteSpace(c.First);
    
    public static string GetCompareOperator(this Span c,string order)
    {
        return  c.IsForward() ? order == SortOrder.Asc ? ">" : "<":
            order == SortOrder.Asc ? "<" : ">";
    }

    public static Record[] ToPage(this Span c, Record[] items, int takeCount)
    {
        if (items.Length == 0) return [];

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
    public static void SetSpan(bool needAddCursor, ImmutableArray<GraphAttribute> attrs, Record[] items,
        IEnumerable<ValidSort> sorts, object? sourceId)
    {
        var arr = sorts.ToArray();
        if (needAddCursor)
        {
            if (items.Length == 0) return;
            SpanHelper.SetCursor(sourceId, items.First(), arr);
            if (items.Length > 1) SetCursor(sourceId, items.Last(), arr);
        }

        foreach (var item in items)
        {
            foreach (var attribute in attrs.GetAttrByType(DisplayType.Lookup))
            {
                if (item.TryGetValue(attribute.Field, out var value) && value is Record record)
                {
                    SetSpan(false, attribute.Selection, [record], [], null);
                }
            }

            foreach (var attribute in attrs.GetAttrByType(DisplayType.Junction))
            {
                if (!item.TryGetValue(attribute.Field, out var value) || value is not Record[] records ||
                    records.Length <= 0) continue;
                var nextSourceId = records.First()[attribute.Junction!.SourceAttribute.Field];
                SetSpan(true, attribute.Selection, records, attribute.Sorts, nextSourceId);
            }
        }
    }

    private static void SetCursor(object? sourceId, Record item, IEnumerable<ValidSort> sorts)
    {
        var dict = new Dictionary<string, object>();
        foreach (var sort in sorts)
        {
            if (item.GetValueByPath<object>(sort.Field, out var val)) dict[sort.Field] = val!;
        }

        if (sourceId is not null)
        {
            dict[SpanConstants.SourceId] = sourceId;
        }
        var cursor = JsonSerializer.Serialize(dict);
        item[SpanConstants.Cursor] = Base64UrlEncoder.Encode(cursor);
    }
    
    public static Result<ValidSpan> ToValid(this Span c, IEnumerable<Attribute> attrs,IAttributeValueResolver resolver)
    {
        if (c.IsEmpty()) return new ValidSpan(c);

        var arr = attrs.ToArray();
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
                    if (field is not null )
                    {
                        if (!resolver.ResolveVal(field, s, out var result))
                        {
                            return Result.Fail($"Fail to cast s to {field.DataType}");
                        }

                        val = result.Value;
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