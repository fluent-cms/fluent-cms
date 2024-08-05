using FluentCMS.Utils.Qs;
using FluentResults;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;
using SqlKata;

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

    public Filter()
    {
        Constraints = new List<Constraint>();
    }
    public static Result<Filter> Parse(Entity entity, string field, Pair[] pairs, Func<Attribute, string, object> cast)
    {
        var filter = new Filter
        {
            FieldName = field,
            Operator = "and",
            Constraints = [],
        };
        var attribute = entity.FindOneAttribute(field);
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

    public Result Apply(Entity entity, Query parentQuery)
    {
        var result = Result.Ok();
        parentQuery.Where(query =>
        {
            var fieldName = entity.Fullname(FieldName);
            foreach (var constraint in Constraints)
            {
                var res = constraint.Apply(query, fieldName, Operator == "or");
                if (res.IsFailed)
                {
                    result = Result.Fail(res.Errors);
                    break;
                }

                query = res.Value;
            }
            return query;
        });
        return result;
    }
}

public class Filters : List<Filter>
{
    private const string QuerystringPrefix = "querystring.";
    private const string TokenPrefix = "token.";
    public Filters(){}
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

    public Result ResolveValues(Entity entity, Func<Attribute, string, object> cast,  Dictionary<string, StringValues>? querystringDictionary,
        Dictionary<string, object>? tokenDictionary)
    {
        foreach (var filter in this)
        {
            var field = entity.FindOneAttribute(filter.FieldName);
            if (field is null)
            {
                return Result.Fail($"Fail to resolve filter: no field ${filter.FieldName} in ${entity.Name}");
            }    
            foreach (var filterConstraint in filter.Constraints)
            {
                var val = filterConstraint.Value;
                if (string.IsNullOrWhiteSpace(val))
                {
                    return Result.Fail($"Fail to resolve Filter, value not set for field{field.Field}");
                }

                var result = val switch
                {
                    _ when val.StartsWith(QuerystringPrefix) => ResolveQuerystringPrefix(field, val),
                    _ when val.StartsWith(TokenPrefix) => ResolveTokenPrefix(val),
                    _ => Result.Ok<object[]>([cast(field,val)]),
                };
                if (result.IsFailed)
                {
                    return Result.Fail(result.Errors);
                }

                filterConstraint.ResolvedValues = result.Value;
            }
        }
        return Result.Ok();
        
        Result<object[]> ResolveQuerystringPrefix(Attribute field, string val)
        {
            var key = val[QuerystringPrefix.Length..];
            if (querystringDictionary is null)
            {
                return Result.Fail($"Fail to resolve filter: no key {key} in query string");
            }

            return querystringDictionary[key].Select(x => cast(field, x!)).ToArray();
        }

        Result<object[]> ResolveTokenPrefix(string val)
        {
            // Implement the logic for resolving TokenPrefix here
            throw new NotImplementedException();
        }
    }

    public Result Apply(Entity entity, Query? query)
    {
        if (query is null)
        {
            return Result.Ok();
        }

        foreach (var filter in this)
        {
            var result = filter.Apply(entity, query);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }

        return Result.Ok();
    }
}
