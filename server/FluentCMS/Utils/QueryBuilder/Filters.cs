using System.Collections.Immutable;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string Operator, ImmutableArray<Constraint> Constraints, bool OmitFail);

public sealed record ValidFilter(AttributeVector Vector, string Operator, ImmutableArray<ValidConstraint> Constraints);

public static class FilterConstants
{
    public const string OmitFailKey = "omitFail";
    public const string LogicalOperatorKey = "operator";
}

public static class FilterHelper
{
    public static Result<Filter> ToFilter(this IValueProvider valueProvider)
    {
        var name = valueProvider.Name();
        return valueProvider.Val().Val.Match<Result<Filter>>(
            s => ToEqualsFilter(name,s),
            strings =>ToInFilter(name, strings) ,
            arr => PairsToFilter(name,arr),
            err => Result.Fail(err)
            );
    }
    
    private static Result<Filter> PairsToFilter(string fieldName, ImmutableArray<(string, object)> pairs)
    {
        //name: {omitFail:true, gt:2, lt:5, operator: and}
        //name: {omitFail:false, eq:3, eq:4, operator: or}
        var omitFail = false;
        var logicalOperator = LogicalOperators.And;
        var constraints = new List<Constraint>();
        foreach (var (key, val) in pairs)
        {
            switch (key)
            {
                case FilterConstants.LogicalOperatorKey:
                    if (val is not string strVal)
                    {
                        return Result.Fail("invalid filter logical operator");
                    }

                    logicalOperator = strVal;
                    break;
                case FilterConstants.OmitFailKey:
                    if (val is not bool boolVal)
                    {
                        return Result.Fail("invalid filter omit fail setting");
                    }

                    omitFail = boolVal;
                    break;
                default:
                    constraints.Add(new Constraint(key, [val.ToString()!]));
                    break;
            }
        }

        return new Filter(fieldName, logicalOperator, [..constraints], omitFail);
    }
    private static Filter ToInFilter(string fieldName, IEnumerable<string> val)
    {
        var constraint = new Constraint(Matches.In, [..val]);
        return new Filter(fieldName, LogicalOperators.And, [constraint], false);
    }
    
    private static Filter ToEqualsFilter(string fieldName, string val)
    {
        var constraint = new Constraint(Matches.EqualsTo, [val]);
        return new Filter(fieldName, LogicalOperators.And, [constraint], false);
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

        var op = strArgs.TryGetValue("operator", out var value) ? value.ToString() : "and";
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