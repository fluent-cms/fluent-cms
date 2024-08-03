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
            set => _fieldName = value.Trim();
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
        public const string SortKey = "sort";

        public Sorts()
        {
        }

        public static Result<Sorts>  Parse(Qs.QsDict qsDict)
        {
            var ret = new Sorts();
                
            if (!qsDict.Dict.TryGetValue(SortKey, out var fields))
            {
                return ret;
            }
            ret.AddRange(fields.Select(field => new Sort
            {
                FieldName = field.Key, Order = field.Values.FirstOrDefault() == "1" ? SortOrder.Asc : SortOrder.Desc,
            }));
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
