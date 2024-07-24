using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;
using SqlKata;

namespace Utils.QueryBuilder;
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

        public Sorts(Dictionary<string, StringValues> queryStringDictionary)
        {
            if (!(queryStringDictionary.TryGetValue(SortFields, out var fields) &&
                  queryStringDictionary.TryGetValue(SortOrders, out var orders) &&
                  fields.Count == orders.Count))
            {
                return;
            }

            for (var i = 0; i < fields.Count; i++)
            {
                var sort = new Sort
                {
                    FieldName = QueryExceptionChecker.StrNotEmpty(fields[i])
                        .ValueOrThrow($"Fail to resolve sorts, not find the {i} field"),
                    Order = orders[i] == "1" ? SortOrder.Desc : SortOrder.Asc
                };
                Add(sort);
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
