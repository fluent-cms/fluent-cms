using System.Collections.Immutable;
using System.Globalization;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string Operator, ImmutableArray<Constraint> Constraints, bool OmitFail);

public sealed record ValidFilter(AttributeVector Vector, string Operator, ImmutableArray<ValidConstraint> Constraints);


public static class FilterConstants
{
    public const string OperatorKey = "operator";
    public const string SetSuffix = "Set";
}

public static class FilterHelper
{
    public static Result<Filter> ToFilter(this IValueProvider valueProvider)
    {
        return valueProvider.Name().EndsWith(FilterConstants.SetSuffix)
            ? valueProvider.ToSimpleFilter()
            : valueProvider.ToComplexFilter();
    }

    private static Result<Filter> ToSimpleFilter(this IValueProvider valueProvider)
    {
        var name = valueProvider.Name()[..^FilterConstants.SetSuffix.Length];
        if (!valueProvider.Vals(out var arr)) return Result.Fail($"Invalid value provided of `{name}`");
        return new Filter(name, LogicalOperators.And, [new Constraint(Matches.In, arr)], false);
    }

    private static Result<Filter> ToComplexFilter(this IValueProvider valueProvider)
    {
        var name = valueProvider.Name();
        if (!valueProvider.Pairs(out var pairs)) return Result.Fail($"Invalid value provided of `{name}`");
        var logicalOperator = LogicalOperators.And;
        var constraints = new List<Constraint>();
        foreach (var (match, val) in pairs)
        {
            if (match == FilterConstants.OperatorKey)
            {
                logicalOperator = val.ToString()!;
            }
            else
            {
                ImmutableArray<string> objs = val switch
                {
                    int[] l => [..l.Select(x => x.ToString())],
                    DateTime[] l => [..l.Select(x => x.ToString(CultureInfo.InvariantCulture))],
                    string[] s => [..s],
                    _ => [val.ToString()!]
                };
                constraints.Add(new Constraint(match, objs));
            }
        }
        return new Filter(name, logicalOperator, [..constraints], false);
    }

    public static async Task<Result> Verify(
        this IEnumerable<Filter> filters,  
        LoadedEntity entity,
        IEntityVectorResolver vectorResolver ,
        IAttributeValueResolver valueResolver
        )
    {
        foreach (var filter in filters)
        {
            var (_,_,vector,errors) = await vectorResolver.ResolveVector(entity, filter.FieldName);
            if (errors is not null)
            {
                return Result.Fail(errors);
            }

            var (_, _, constraintsError) =  filter.Constraints.Verify(vector.Attribute, valueResolver);
            if (constraintsError is not null)
            {
                return Result.Fail(constraintsError);
            }
        }

        return Result.Ok();
    }
    public static async Task<Result<ImmutableArray<ValidFilter>>> ToValid(
        this IEnumerable<Filter> filters,  
        LoadedEntity entity,
        QueryStrArgs? args,
        IEntityVectorResolver vectorResolver,  
        IAttributeValueResolver valueResolver  
        )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            var (_, _, vector, vectorError) = await vectorResolver.ResolveVector(entity,filter.FieldName);
            if (vectorError is not null)
            {
                return Result.Fail(vectorError);
            }

            var (_, _, constraints, constraintErrors) = filter.Constraints.Resolve(vector.Attribute, args,valueResolver, filter.OmitFail);
            if (constraintErrors is not null)
            {
                return Result.Fail(constraintErrors);
            }
            if (constraints.Length > 0)
            {
                ret.Add(new ValidFilter(vector, filter.Operator, constraints));
            }
        }
        return ret.ToImmutableArray();
    }

    public static async Task<Result<ImmutableArray<ValidFilter>>> Parse(
        LoadedEntity entity, 
        Dictionary<string, QueryStrArgs> dictionary, 
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

            var (_,_,filter, errors) = await Parse(entity, key, value, vectorResolver, valueResolver);
            if (errors is not null)
            {
                return Result.Fail(errors);
            }

            ret.Add(filter);

        }

        return ret.ToImmutableArray();
    }

    private static async Task<Result<ValidFilter>> Parse(LoadedEntity entity, string field, QueryStrArgs strArgs, 
        IEntityVectorResolver vectorResolver, IAttributeValueResolver valueResolver
        )
    {
        var (_, _, vector, errors) = await vectorResolver.ResolveVector(entity, field);
        if (errors is not null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}, errors: {errors}");
        }

        var op = strArgs.TryGetValue(FilterConstants.OperatorKey, out var value) ? value.ToString() : "and";
        var constraints = new List<ValidConstraint>();
        foreach (var (match, values) in strArgs.Where(x =>x.Key != "operator"))
        {
            if (!valueResolver.ResolveVal(vector.Attribute, values.ToString(), out var obj))
            {
                return Result.Fail($"Failed to case {values.ToString()} to {vector.Attribute.DataType}");

            }
            constraints.Add(new ValidConstraint(match, [obj!]));
        }
        return new ValidFilter(vector, op, [..constraints]);
    }
}