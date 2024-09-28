using System.Text.Json.Serialization;
using FluentResults;

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

       
    }

    public class Sorts : List<Sort>
    {
        public const string SortKey = "sort";

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

        public Sorts ReverseOrder()
        {
            var ret = new Sorts();
            ret.AddRange(this.Select(sort => new Sort
                { FieldName = sort.FieldName, Order = sort.Order == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc }));
            return ret;
        }
    }
