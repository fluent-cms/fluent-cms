using System.Collections.Immutable;
using FluentCMS.Utils.DictionaryExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Constraint(string Match, ImmutableArray<string> Value);
public sealed record ValidConstraint(string Match, ImmutableArray<object> Values);

public static class ConstraintsHelper
{
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
                if (!se.StartsWith(QueryConstants.VariablePrefix) && !resolver.ResolveVal(attribute, se, out var _))
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
        StrArgs? args,
        IAttributeValueResolver resolver
        )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            var resolveValResult = ResolveValues(fromValues, attribute, args,resolver);
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
        StrArgs? args, IAttributeValueResolver resolver)
    {
        var list = new List<object>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue.StartsWith(QueryConstants.VariablePrefix))
            {
                var key = fromValue[QueryConstants.VariablePrefix.Length..];
                if (args.GetStrings(key, out var strings))
                {
                    list.AddRange(strings);
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
}

public static class MatchTypes
{
    public const string MatchAll = "matchAll";
    public const string MatchAny = "matchAny";
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
    
    
    public static readonly ImmutableHashSet<string> SingleInt = [EqualsTo,Lt,Lte,Gt,Gte];
    public static readonly ImmutableHashSet<string> MultiInt = [Between,In,NotIn];
    
    public static readonly ImmutableHashSet<string> SingleStr = [
        EqualsTo,Lt,Lte,Gt,Gte,
        StartsWith, Contains, NotContains, EndsWith,EqualsTo
    ];
    public static readonly ImmutableHashSet<string> MultiStr = [Between,In,NotIn];
    
    public static readonly ImmutableHashSet<string> SingleDate = [DateIs,DateIsNot,DateBefore, DateAfter];
    public static readonly ImmutableHashSet<string> MultiDate = [Between,In,NotIn];
    
    public static readonly ImmutableHashSet<string> Multi = [Between,In,NotIn];
    public static readonly ImmutableHashSet<string> Single = [
        StartsWith, Contains, NotContains, EndsWith,
        EqualsTo,Lt,Lte,Gt,Gte,
        DateIs,DateIsNot,DateBefore, DateAfter
    ];
}