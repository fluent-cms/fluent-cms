using System.Collections.Immutable;
using FluentCMS.Utils.DictionaryExt;
using FluentCMS.Utils.ResultExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

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

public sealed record Constraint(string Match, ImmutableArray<string> Value);
public sealed record ValidConstraint(string Match, ImmutableArray<ValidValue> Values);

public static class ConstraintsHelper
{
    public static Result<ValidConstraint[]> ResolveValues(
        this IEnumerable<Constraint> constraints,
        Attribute attribute,
        IAttributeValueResolver resolver
    )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            if (!ResolveValues(fromValues, attribute, resolver).Try(out var values, out var err))
            {
                return Result.Fail(err);
            }

            if (values.Length > 0)
            {
                ret.Add(new ValidConstraint(match, [..values]));
            }
        }

        return ret.ToArray();
    }

    public static Result<ValidConstraint[]> ReplaceVariables(
        this IEnumerable<ValidConstraint> constraints,
        Attribute attribute,
        StrArgs? args,
        IAttributeValueResolver resolver
    )
    {
        var ret = new List<ValidConstraint>();
        foreach (var (match, fromValues) in constraints)
        {
            if (!ReplaceVariables(fromValues, attribute, args, resolver).Try(out var values, out var err))
            {
                return Result.Fail(err);
            }

            if (values.Length > 0)
            {
                ret.Add(new ValidConstraint(match, [..values]));
            }
        }

        return ret.ToArray();
    }

    
    private static Result<ValidValue[]> ReplaceVariables(IEnumerable<ValidValue> fromValues, Attribute attribute,
        StrArgs? args, IAttributeValueResolver resolver)
    {
        var list = new List<ValidValue>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue.Value is string s && s.StartsWith(QueryConstants.VariablePrefix))
            {
                if (args is null)
                {
                    return Result.Fail($"can not resolve {fromValue} when replace filter");
                }
                
                foreach (var se in args.GetVariableStr(s, QueryConstants.VariablePrefix))
                {
                    if (se is not null && resolver.ResolveVal(attribute, se, out var obj))
                    {
                        list.Add(obj);
                    }
                    else
                    {
                        return Result.Fail($"can not cast value {se} to {attribute.DataType}");
                    }
                }
            }
            else
            {
                list.Add(fromValue);
            }
        }

        return list.ToArray();
    }

    private static Result<ValidValue[]> ResolveValues(IEnumerable<string> fromValues, Attribute attribute, IAttributeValueResolver resolver)
    {
        var list = new List<ValidValue>();

        foreach (var fromValue in fromValues)
        {
            if (fromValue.StartsWith(QueryConstants.VariablePrefix))
            {
                list.Add(new ValidValue(fromValue));
                continue;
            }
            if (!resolver.ResolveVal(attribute, fromValue, out var dbTypeValue))
            {
                return Result.Fail("can not cast value " + fromValue + " to " + attribute.DataType);
            }
            list.Add(dbTypeValue);
        }
        return list.ToArray();
    }
}

