using System.Collections.Immutable;
using System.Globalization;
using FluentCMS.Utils.Graph;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using MongoDB.Driver.Linq;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string MatchType, ImmutableArray<Constraint> Constraints);

public sealed record ValidFilter(AttributeVector Vector, string MatchType, ImmutableArray<ValidConstraint> Constraints);


public static class FilterConstants
{
    public const string MatchTypeKey = "matchType";
    public const string SetSuffix = "Set";
    public const string FilterExprKey = "filterExpr";
    public const string FieldKey = "field";
    public const string ClauseKey = "clause";
}

public static class FilterHelper
{
    public static Result<Filter[]> ParseFilterExpr(IDataProvider provider)
    {
        if (!provider.TryGetNodes(out var nodes))
        {
            return Result.Fail("Unable to parse filter expression of field.");
        }
        
        var ret = new List<Filter>();
        foreach (var node in nodes)
        {
            if (!node.TryGetVal(FilterConstants.FieldKey,out var fieldName))
            {
                return Result.Fail("Unable to parse filter expression, field is not set");
            }

            if (!node.TryGetPairs(FilterConstants.ClauseKey, out var clauses))
            {
                return Result.Fail($"Unable to parse filter expression, failed to find clause of `{fieldName}` ");
            }
            ret.Add(ParseComplexFilter(fieldName, clauses));
        }
        return ret.ToArray();
    }

    public static Result<Filter> ParseFilter<T>(T valueProvider)
    where T: IDataProvider
    {
        return valueProvider.Name().EndsWith(FilterConstants.SetSuffix)
            ? ParseSimpleFilter(valueProvider)
            : Complex();

        Result<Filter> Complex()
        {
            if (!valueProvider.TryGetPairs(out var pairs))
            {
                return Result.Fail($"Fail to parse filter {valueProvider.Name()}.");
            }
            return ParseComplexFilter(valueProvider.Name(), pairs);
        }
    }

    private static Result<Filter> ParseSimpleFilter<T>(T valueProvider)
    where T: IDataProvider
    {
        var name = valueProvider.Name()[..^FilterConstants.SetSuffix.Length];
        if (!valueProvider.TryGetVals(out var arr)) 
            return Result.Fail($"Fail to parse simple filter, Invalid value provided of `{name}`");
        var constraint = new Constraint(Matches.In, [..arr]);
        return new Filter(name, MatchTypes.MatchAll, [constraint]);
    }

    private static Filter ParseComplexFilter(string field, IEnumerable<StrPair> clauses)
    {
        var matchType = MatchTypes.MatchAll;
        var constraints = new List<Constraint>();
        foreach (var (match, val) in clauses)
        {
            if (match == FilterConstants.MatchTypeKey)
            {
                matchType = val.First();
            }
            else
            {
                constraints.Add(new Constraint(match, [..val]));
            }
        }
        return new Filter(field, matchType, [..constraints]);
    }

    public static async Task<Result> Verify(
        this IEnumerable<Filter> filters,
        LoadedEntity entity,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        foreach (var filter in filters)
        {
            if (!(await vectorResolver.ResolveVector(entity, filter.FieldName))
                .Try(out var vector, out var resolveErr))
            {
                return Result.Fail(resolveErr);
            }

            if (!filter.Constraints.Verify(vector.Attribute, valueResolver)
                    .Try(out var verifyErr))
            {
                return Result.Fail(verifyErr);
            }
        }

        return Result.Ok();
    }

    public static async Task<Result<ValidFilter[]>> ToValid(
        this IEnumerable<Filter> filters,
        LoadedEntity entity,
        StrArgs? args,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            if (!(await vectorResolver.ResolveVector(entity, filter.FieldName))
                .Try(out var vector, out var resolveErr))
            {
                return Result.Fail(resolveErr);
            }

            if (!filter.Constraints.Resolve(vector.Attribute, args, valueResolver)
                    .Try(out var constraints, out var constraintsErr))
            {
                return Result.Fail(constraintsErr);
            }

            if (constraints.Length > 0)
            {
                ret.Add(new ValidFilter(vector, filter.MatchType, constraints));
            }
        }

        return ret.ToArray();
    }

    public static async Task<Result<ValidFilter[]>> Parse(
        LoadedEntity entity,
        Dictionary<string, StrArgs> dictionary,
        IEntityVectorResolver vectorResolver, IAttributeValueResolver valueResolver
    )
    {
        var ret = new List<ValidFilter>();
        foreach (var (key, value) in dictionary)
        {
            if (key == SortConstant.SortKey)
            {
                continue;
            }

            var (_, _, filter, errors) = await Parse(entity, key, value, vectorResolver, valueResolver);
            if (errors is not null)
            {
                return Result.Fail(errors);
            }

            ret.Add(filter);

        }

        return ret.ToArray();
    }

    private static async Task<Result<ValidFilter>> Parse(
        LoadedEntity entity,
        string field,
        StrArgs strArgs,
        IEntityVectorResolver vectorResolver,
        IAttributeValueResolver valueResolver
    )
    {
        var (_, _, vector, errors) = await vectorResolver.ResolveVector(entity, field);
        if (errors is not null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}, errors: {errors}");
        }

        var op = strArgs.TryGetValue(FilterConstants.MatchTypeKey, out var value) ? value.ToString() : "and";
        var constraints = new List<ValidConstraint>();
        foreach (var (match, values) in strArgs.Where(x => x.Key != "operator"))
        {
            var list = new List<object>();
            foreach (var stringValue in values)
            {
                if (stringValue is not null && valueResolver.ResolveVal(vector.Attribute, stringValue, out var obj) && obj is not null)
                {
                    list.Add(obj);
                }
                else
                {
                    return Result.Fail($"Failed to case {values.ToString()} to {vector.Attribute.DataType}");
                }
            }
            constraints.Add(new ValidConstraint(match, [..list]));
        }
        return new ValidFilter(vector, op, [..constraints]);
    }
}