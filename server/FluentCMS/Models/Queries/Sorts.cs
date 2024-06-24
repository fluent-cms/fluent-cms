using System.Text.Json;
using System.Text.Json.Serialization;
using FluentCMS.Utils.Base64Url;
using Microsoft.Extensions.Primitives;
using SqlKata;

namespace  FluentCMS.Models.Queries
{
    public enum SortOrder
    {
        Asc,
        Desc,
    }

    public class Sort
    {
        public string FieldName { get; set; } = "";
        [JsonIgnore]
        public SortOrder Order { get; set; }
    }

    public class Sorts : List<Sort>
    {
        const string SortFields = "s[field]";
        const string SortOrders = "s[order]";
        public Sorts(){}

        public Sorts(Dictionary<string, StringValues> qs)
        {
            if (qs.ContainsKey(SortFields) && qs.ContainsKey(SortOrders))
            {
                var fields =  qs[SortFields];
                var orders = qs[SortOrders];
                if (fields.Count == orders.Count)
                {
                    for(var i = 0; i<  fields.Count; i ++)
                    {
                        var sort = new Sort
                        {
                            FieldName = fields[i],
                            Order = orders[i] == "1" ? SortOrder.Desc : SortOrder.Asc
                        };
                        Add(sort);
                    }
                }
            }
        }
        public IDictionary<string, object>? ParseCursor(string cursor)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                return null;
            }

            byte[] bs;
            try
            {
                bs = Base64UrlEncoder.Decode(cursor);
            }
            catch (Exception)
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, object>>(bs);

        }

        public string? GenerateCursor(List<Dictionary<string, object>>? items)
        {
            if (items is null || items.Count == 0)
            {
                return null;
            }

            var lastItem = items.Last();
            var item = new Dictionary<string, object>();
            foreach (var field in this.Select(x=>x.FieldName))
            {
                if (lastItem.ContainsKey(field))
                {
                    item[field] = lastItem[field];
                }
            }

            return JsonSerializer.Serialize(item);
        }

        public void Apply(Entity entity, Query? query)
        {
            if (query is null)
            {
                return;
            }
            foreach (var sort in this)
            {
                if (sort.Order == SortOrder.Desc)
                {
                    query.OrderByDesc(entity.Fullname(sort.FieldName));
                }
                else
                {
                    query.OrderBy(entity.Fullname(sort.FieldName));
                }
            }
        }
    }
}
