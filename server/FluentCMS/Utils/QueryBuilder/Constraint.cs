using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Constraint(string Match, string Value);
public sealed record ValidConstraint(string Match, object[] Values);

public static class ConstraintsHelper
{
    private const string QuerystringPrefix = "qs.";

    public static Result<ValidConstraint[]> Resolve(
        this Constraint[] constraints, 
        bool ignoreResolveError,
        string entityName,
        BaseAttribute attribute,  
        Dictionary<string, StringValues>? querystringDictionary,
        Func<string, string, object> cast)
    {
        var ret = new List<ValidConstraint>();
        foreach (var constraint in constraints)
        {
            var val = constraint.Value;
            if (string.IsNullOrWhiteSpace(val))
            {
                return Result.Fail($"Fail to resolve Filter, value not set for field {entityName}.{attribute.Field}");
            }

            if (val.StartsWith(QuerystringPrefix))
            {
                var res = ResolveFromQueryString(val);
                if (res.IsSuccess)
                {
                    var arr = res.Value.Select(x => cast(attribute.Field, x)).ToArray();
                    ret.Add(new ValidConstraint(constraint.Match, arr));
                }
                else if (!ignoreResolveError)
                {
                    return Result.Fail(res.Errors);
                }//else ignore this constraint  
            }
            else
            {
                ret.Add(new ValidConstraint(constraint.Match, [cast(attribute.Field,val)]));
            }
        }

        return ret.ToArray();
        

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