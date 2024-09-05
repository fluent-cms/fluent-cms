using System.Text.Json;
using FluentCMS.Utils.Base64Url;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed class Cursor
{
    public string First { get; set; } = "";
    public string Last { get; set; } = "";

    public Record? BoundaryItem { get; set; }

    public object? BoundaryValue(string fld) => BoundaryItem?[fld];
    private bool IsEmpty => string.IsNullOrWhiteSpace(First) && string.IsNullOrWhiteSpace(Last);
    private bool IsForward => !string.IsNullOrWhiteSpace(Last) || string.IsNullOrWhiteSpace(First);
    
    public string GetCompareOperator(Sort s)
    {
        return  IsForward ? s.Order == SortOrder.Asc ? ">" : "<":
            s.Order == SortOrder.Asc ? "<" : ">";
    }

    public Result<Cursor> GetNextCursor(Record[] items, Sorts? sorts, bool hasMore)
    {
        if (sorts is null)
        {
            return Result.Fail("Can not generate next cursor, sort was not set");
        }

        if (items.Length == 0)
        {
            return Result.Fail("No result, can not generate cursor");
        }

        var (hasPrevious, hasNext) = (hasMore, First,Last) switch
        {
            (true, "", "") => (false, true), // home page, should not has previous
            (false, "", "") => (false, false), // home page
            (true, _, _) => (true, true), // no matter click next or previous, show both
            (false, _, "") => (false, true), // click preview, should have next
            (false, "", _) => (true, false), // click next, should nave previous
            _ => (false, false)
        };
        
        return new Cursor
        {
            First = hasPrevious? EncodeRecord(items.First(), sorts):"",
            Last = hasNext? EncodeRecord(items.Last(), sorts):""
        };
    }

    private static string EncodeRecord(Record item, Sorts sorts)
    {
        var dict = new Dictionary<string, object>();
        foreach (var field in sorts.Select(x => x.FieldName))
        {
            if (item.TryGetValue(field, out var val))
            {
                dict[field] = val;
            }
        }

        var cursor = JsonSerializer.Serialize(dict);
        return Base64UrlEncoder.Encode(cursor);
    }

    public Result ResolveBoundaryItem(Entity entity, Func<Attribute, string, object> cast)
    {
        if (IsEmpty) return Result.Ok();
        try
        {
            var recordStr = IsForward ? Last : First;
            recordStr = Base64UrlEncoder.Decode(recordStr);
            var element = JsonSerializer.Deserialize<JsonElement>(recordStr);
            var parseRes = entity.Parse(element, cast);
            if (parseRes.IsFailed)
            {
                return Result.Fail(parseRes.Errors);
            }
            BoundaryItem = parseRes.Value;
            return Result.Ok();
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
    }
}