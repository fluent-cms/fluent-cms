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
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
