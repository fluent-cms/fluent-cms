using System.Collections.Immutable;
using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Constraint(string Match, string Value);
public sealed record ValidConstraint(string Match, object[] Values);

public static class ConstraintsHelper
{
    private const string QuerystringPrefix = "qs.";

    public static Result Verify(this IEnumerable<Constraint> constraints, Attribute attribute, IAttributeResolver resolver)
    {
        foreach (var (match, value) in constraints)
        {
            if (string.IsNullOrWhiteSpace(value) )
            {
                return Result.Fail($"value not set for field {attribute.Field}");
            }
            if (!resolver.GetAttrVal(attribute, value,out var _))
            {
                return Result.Fail($"Can not cast value `{value}` of `{attribute.Field}` to `{attribute.DataType}`");
            }
        }
        return Result.Ok();
    }
    public static Result<ImmutableArray<ValidConstraint>> Resolve(
        this IEnumerable<Constraint> constraints, 
        Attribute attribute,  
        Dictionary<string, StringValues>? querystringDictionary,
        IAttributeResolver resolver,
        bool ignoreResolveError
        )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, val) in constraints)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                return Result.Fail($"Fail to resolve Filter, value not set for field {attribute.Field}");
            }
            if (val.StartsWith(QuerystringPrefix))
            {
                var (_,_, values, err) = ResolveFromQueryString(val);
                if (err is null)
                {
                    var arr = new List<object>();
                    foreach (var value in values)
                    {
                        if (!resolver.GetAttrVal(attribute, value, out var dbTypeValue))
                        {
                            return Result.Fail("can not cast value " + value + " to " + attribute.DataType);
                        }
                        arr.Add(dbTypeValue!);
                    }
                    ret.Add(new ValidConstraint(match, [..arr]));
                }
                else if (!ignoreResolveError)
                {
                    return Result.Fail(err);
                }//else ignore this constraint  
            }
            else
            {
                if (!resolver.GetAttrVal(attribute, val, out var dbTypeValue))
                {
                    return Result.Fail("can not cast value " + val + " to " + attribute.DataType);
                }

                ret.Add(new ValidConstraint(match, [dbTypeValue!]));
            }
        }

        return ret.ToImmutableArray();
        

        Result<string[]> ResolveFromQueryString (string val)
        {
            var key = val[QuerystringPrefix.Length..];
            if (querystringDictionary is null||!querystringDictionary.TryGetValue(key, out var vals))
            {
                return Result.Fail($"Fail to resolve filter: no key {key} in query string");
            }
            return vals.ToArray()!;
        } 
    }
}

public static class LogicalOperators
{
    public const string And = "and";
    public const string Or = "or";
}
public static class Matches
{
    public const string Between = "between";
    public const string StartsWith = "startsWith";
    public const string Contains = "contains";
    public const string NotContains = "notContains";
    public const string EndsWith = "endsWith";
    public const string EqualsTo = "equals";
    public const string NotEquals = "notEquals";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string Lt = "lt";
    public const string Lte = "lte";
    public const string Gt = "gt";
    public const string Gte = "gte";
    public const string DateIs = "dateIs";
    public const string DateIsNot = "dateIsNot";
    public const string DateBefore = "dateBefore";
    public const string DateAfter = "dateAfter";
}