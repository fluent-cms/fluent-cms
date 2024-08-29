using System.Text.Json.Serialization;
using FluentCMS.Utils.Qs;
using FluentResults;
using Microsoft.Extensions.Primitives;
namespace FluentCMS.Utils.QueryBuilder;
using System.Collections.Generic;

public sealed class Filter
{
    private string _fieldName = "";
    public string FieldName
    {
        get => _fieldName;
        set => _fieldName = value.Trim();
    }

    public string Operator { get; set; } = "";

    public List<Constraint> Constraints { get; set; }

    public bool OmitFail { get; set; }
    public Filter()
    {
        Constraints = new List<Constraint>();
    }

    [JsonIgnore]
    public bool IsOr => Operator == "or";
    
    public static Result<Filter> Parse(Entity entity, string field, Pair[] pairs, Func<Attribute, string, object> cast)
    {
        var filter = new Filter
        {
            FieldName = field,
            Operator = "and",
            Constraints = [],
        };
        var attribute = entity.Attributes.FindOneAttribute(field);
        if (attribute is null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}");
        }
        
        foreach (var pair in pairs)
        {
            foreach (var pairValue in pair.Values)
            {
                if (pair.Key == "operator")
                {
                    filter.Operator = pair.Values.First();
                    continue;
                }
                
                filter.Constraints.Add(new Constraint
                {
                    Match = pair.Key,
                    ResolvedValues = [cast(attribute,pairValue)],
                });
            }
        }
        return filter;
    }
}

public class Filters : List<Filter>
{
    private const string QuerystringPrefix = "qs.";
    public static Result<Filters> Parse(Entity entity, QsDict qsDict, Func<Attribute, string, object> cast)
    {
        var ret = new Filters();
        foreach (var pair in qsDict.Dict)
        {
            if (pair.Key == Sorts.SortKey)
            {
                continue;
            }
            var result = Filter.Parse(entity, pair.Key, pair.Value.ToArray(), cast);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
            ret.Add(result.Value);
        }
        return ret;
    }

    public Result ResolveValues(Entity entity, Func<Attribute, string, object> cast,  Dictionary<string, StringValues>? querystringDictionary)
    {
        for (var i = Count -1; i >= 0; i -- )
        {
            var filter = this[i];
            var field = entity.Attributes.FindOneAttribute(filter.FieldName);
            if (field is null)
            {
                return Result.Fail($"Fail to resolve filter: no field ${filter.FieldName} in ${entity.Name}");
            }    
            foreach (var filterConstraint in filter.Constraints)
            {
                var val = filterConstraint.Value;
                if (string.IsNullOrWhiteSpace(val))
                {
                    return Result.Fail($"Fail to resolve Filter, value not set for field {field.Field}");
                }

                var result = val switch
                {
                    _ when val.StartsWith(QuerystringPrefix) => ResolveQuerystringPrefix(field, val),
                    _ => Result.Ok<object[]>([cast(field,val)]),
                };
                if (result.IsFailed)
                {
                    if (filter.OmitFail)
                    {
                        RemoveAt(i);
                    }
                    else
                    {
                        return Result.Fail(result.Errors);
                    }
                }
                else
                {
                    filterConstraint.ResolvedValues = result.Value;
                }

            }
        }
        return Result.Ok();
        
        Result<object[]> ResolveQuerystringPrefix(Attribute field, string val)
        {
            var key = val[QuerystringPrefix.Length..];
            if (querystringDictionary is null||!querystringDictionary.TryGetValue(key, out var vals))
            {
                return Result.Fail($"Fail to resolve filter: no key {key} in query string");
            }
            return vals.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => cast(field, x!)).ToArray();
        }
    }
}
