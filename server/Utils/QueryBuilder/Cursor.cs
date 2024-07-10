using System.Text.Json;
using FluentCMS.Utils.Base64Url;
using SqlKata;

namespace Utils.QueryBuilder;

public class Cursor
{
    public string First { get; set; } = "";
    public string Last { get; set; } = "";
    public int Limit { get; set; }

    public bool GetFirstAndLastCursor(Record[]? items, Sorts? sorts, bool hasMore, 
        out string first, out bool hasPrevious,
        out string last, out bool hasNext)
    {
        first = "";
        last = "";
        hasNext = false;
        hasPrevious = false;
        
        if (sorts is null || items is null || items.Length == 0)
        {
            return false;
        }

        if (hasMore)
        {
            if (string.IsNullOrWhiteSpace(Last) && string.IsNullOrWhiteSpace(First))
            {
                // the home page, keep hasPrevious false 
                hasNext = true;
                last = GenerateCursor(items.Last(), sorts);
            }
            else
            {
                hasNext = true;
                hasPrevious = true;
                first = GenerateCursor(items.First(), sorts);
                last = GenerateCursor(items.Last(), sorts);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Last))
            {
                // click next, so must has previous
                hasPrevious = true;
                first = GenerateCursor(items.First(), sorts);
            }else if (!string.IsNullOrWhiteSpace(First))
            {
                // click previous, so must has next
                hasNext = true;
                last = GenerateCursor(items.Last(), sorts);
            }
        }
        return true;
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
        if (sorts.Count == 1)
        {
            var sort = sorts[0];
            query.Where(entity.Fullname(sort.FieldName), sort.GetCompareOperator(forNextPage), dict[sort.FieldName]);
            return;
        }

        if (sorts.Count > 2)
        {
            throw new Exception("Only Support order by two field");
        }

        var first = sorts.First();
        var last = sorts.Last();
        query.Where(q =>
        {
            q.Where(entity.Fullname(first.FieldName), first.GetCompareOperator(forNextPage), dict[first.FieldName]);
            q.Or();
            q.Where(entity.Fullname(first.FieldName), dict[first.FieldName]);
            q.Where(entity.Fullname(last.FieldName), last.GetCompareOperator(forNextPage), dict[last.FieldName]);
            return q;
        });
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