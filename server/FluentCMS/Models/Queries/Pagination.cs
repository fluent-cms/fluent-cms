using System.Text.Json;
using FluentCMS.Utils.Base64Url;
using SqlKata;

namespace FluentCMS.Models.Queries;

public class Pagination
{
    public int First { get; set; }
    public int Rows { get; set; }
    public string? Cursor { get; set; }

    public static string? GenerateCursor(IDictionary<string, object>[]? items, Sorts? sorts)
    {
        if (sorts is null)
        {
            return null;
        }
        var lastItem = items?.LastOrDefault();
        if (lastItem is null)
        {
            return null;
        }
        
        var item = new Dictionary<string, object>();
        foreach (var field in sorts.Select(x=>x.FieldName))
        {
            if (lastItem.TryGetValue(field, out var val))
            {
                item[field] = val;
            }
        }
        var bs = JsonSerializer.Serialize(item);
        return Base64UrlEncoder.Encode(bs);
    }
    
    public void Apply(Entity entity, Query? query, Sorts? sorts)
    {
        if (query is null)
        {
            return;
        }

        var limit = Rows;
        if (limit == 0)
        {
            limit = 1000;
        }
        query.Limit(limit);

        if (Cursor is not null && sorts is not null)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(Base64UrlEncoder.Decode(Cursor));
            foreach (var sort in sorts)
            {
                var op = sort.Order == SortOrder.Asc ? ">" : "<";
                query.Where(entity.Fullname(sort.FieldName), op, dict[sort.FieldName]);
            }
        }
        else
        {
            query.Offset(First);
        }

    }
}
