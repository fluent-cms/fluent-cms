using System.Collections.Immutable;
using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Constraint(string Match, ImmutableArray<string> Value);
public sealed record ValidConstraint(string Match, ImmutableArray<object> Values);

public static class ConstraintsHelper
{
    private const string QuerystringPrefix = "qs.";

    public static Result Verify(this IEnumerable<Constraint> constraints, Attribute attribute,
        IAttributeValueResolver resolver)
    {
        foreach (var (_, value) in constraints)
        {
            if (value.Length == 0)
            {
                return Result.Fail($"value not set for field {attribute.Field}");
            }

            foreach (var se in value)
            {
                if (!se.StartsWith(QuerystringPrefix) && !resolver.ResolveVal(attribute, se, out var _))
                {
                    return Result.Fail(
                        $"Can not cast value `{value}` of `{attribute.Field}` to `{attribute.DataType}`");
                }
            }
        }
        return Result.Ok();
    }

    public static Result<ImmutableArray<ValidConstraint>> Resolve(
        this IEnumerable<Constraint> constraints, 
        Attribute attribute,  
        QueryStrArgs? args,
        IAttributeValueResolver resolver,
        bool ignoreResolveError
        )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            var resolveValResult = ResolveValues(fromValues, attribute, args,resolver, ignoreResolveError);
            if (!resolveValResult.IsSuccess)
            {
                return Result.Fail(resolveValResult.Errors);
            }

            if (resolveValResult.Value.Length > 0)
            {
                ret.Add(new ValidConstraint(match, resolveValResult.Value));
            }
        }
        return ret.ToImmutableArray();
    }

    private static Result<ImmutableArray<object>> ResolveValues(IEnumerable<string> fromValues, Attribute attribute,
        QueryStrArgs? args, IAttributeValueResolver resolver, bool ignoreResolveError)
    {
        var list = new List<object>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue.StartsWith(QuerystringPrefix))
            {
                var (_, _, values, err) = ResolveFromQueryString(fromValue, args);
                if (err is not null && !ignoreResolveError)
                {
                    return Result.Fail(err);
                }

                foreach (var value in values??[])
                {
                    if (!resolver.ResolveVal(attribute, value, out var dbTypeValue))
                    {
                        return Result.Fail("can not cast value " + value + " to " + attribute.DataType);
                    }

                    list.Add(dbTypeValue!);
                }
            }
            else
            {
                if (!resolver.ResolveVal(attribute, fromValue, out var dbTypeValue))
                {
                    return Result.Fail("can not cast value " + fromValue + " to " + attribute.DataType);
                }
                list.Add(dbTypeValue!);
            }
        }
        return list.ToImmutableArray();
    }

    private static Result<string[]> ResolveFromQueryString (string val, QueryStrArgs? args)
    {
        var key = val[QuerystringPrefix.Length..];
        if (args is null||!args.TryGetValue(key, out var vals))
        {
            return Result.Fail($"Fail to resolve constraint value, can not find `{key}` in query string");
        }
        return vals.ToArray()!;
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