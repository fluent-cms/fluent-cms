using System.Text.Json;
using FluentCMS.Utils.Base64Url;
using FluentResults;
using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;

public sealed class Cursor
{
    public string First { get; set; } = "";
    public string Last { get; set; } = "";

    public int Limit { get; set; }

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
            First = hasPrevious? GenerateCursor(items.First(), sorts):"",
            Last = hasNext? GenerateCursor(items.Last(), sorts):""
        };
    }

    private static string GenerateCursor(Record item, Sorts sorts)
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


    private void Apply(Entity entity, Query? query, Sorts? sorts, string cursor, bool forNextPage)
    {
        if (cursor == "" || sorts is null || sorts.Count == 0 || query is null)
        {
            return;
        }

        cursor = Base64UrlEncoder.Decode(cursor);
        var element = JsonSerializer.Deserialize<JsonElement>(cursor);
        var dict = RecordParser.Parse(element, entity);
        switch (sorts.Count)
        {
            case 1:

                var sort = sorts[0];
                query.Where(entity.Fullname(sort.FieldName), sort.GetCompareOperator(forNextPage),
                    dict[sort.FieldName]);
                break;
            case 2:
                var first = sorts.First();
                var last = sorts.Last();
                query.Where(q =>
                {
                    q.Where(entity.Fullname(first.FieldName), first.GetCompareOperator(forNextPage),
                        dict[first.FieldName]);
                    q.Or();
                    q.Where(entity.Fullname(first.FieldName), dict[first.FieldName]);
                    q.Where(entity.Fullname(last.FieldName), last.GetCompareOperator(forNextPage),
                        dict[last.FieldName]);
                    return q;
                });
                break;
            default:
                throw new Exception("Only Support sort more than 2 fields");
        }
    }

    public void Apply(Entity entity, Query? query, Sorts? sorts)
    {
        if (query is null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(Last))
        {
            Apply(entity, query, sorts, Last, true);
        }
        else if (!string.IsNullOrWhiteSpace(First))
        {
            Apply(entity, query, sorts, First, false);
        }

        query.Limit(Limit);
    }
}