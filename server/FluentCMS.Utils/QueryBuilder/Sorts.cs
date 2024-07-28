using System.Text.Json.Serialization;
using FluentResults;
using Microsoft.Extensions.Primitives;
using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;
    public enum SortOrder
    {
        Asc,
        Desc,
    }

    public class Sort
    {
        private string _fieldName = "";

        public string FieldName
        {
            get => _fieldName;
            set => _fieldName = NameFormatter.LowerNoSpace(value);
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SortOrder Order { get; set; }

        public string GetCompareOperator(bool forNextPage)
        {
            return   forNextPage ? Order == SortOrder.Asc ? ">" : "<":
                Order == SortOrder.Asc ? "<" : ">";
        }
    }

    public class Sorts : List<Sort>
    {
        const string SortFields = "s[field]";
        const string SortOrders = "s[order]";

        public Sorts()
        {
        }

        public static Result<Sorts>  Parse(Dictionary<string, StringValues> queryStringDictionary)
        {
            var ret = new Sorts();
            if (!(queryStringDictionary.TryGetValue(SortFields, out var fields) &&
                  queryStringDictionary.TryGetValue(SortOrders, out var orders) &&
                  fields.Count == orders.Count))
            {
                return ret;
            }


            for (var i = 0; i < fields.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(fields[i]))
                {
                    return Result.Fail($"Fail to resolve sorts, not find the {i} field");
                }

                var sort = new Sort
                {
                    FieldName = fields[i]!,
                    Order = orders[i] == "1" ? SortOrder.Desc : SortOrder.Asc
                };
                ret.Add(sort);
            }

            return ret;
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
