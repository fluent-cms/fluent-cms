using System.Collections.Immutable;
using FluentResults;
using Microsoft.Extensions.Primitives;

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
    
    public static async Task<Result<ImmutableArray<ValidFilter>>> ToValid(
        this IEnumerable<Filter> filters,  
        LoadedEntity entity,
        Dictionary<string, StringValues>? querystringDictionary,
        ResolveVectorDelegate resolveVector 
        )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            var (_, _, vector, vectorError) = await resolveVector(entity,filter.FieldName);
            if (vectorError is not null)
            {
                return Result.Fail(vectorError);
            }

            var (_, _, constraints, constraintErrors) = filter.Constraints.Resolve(vector.Attribute, filter.OmitFail,querystringDictionary);
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

    public static async Task<Result<ImmutableArray<ValidFilter>>> Parse(LoadedEntity entity,
        Dictionary<string, Dictionary<string, StringValues>> dictionary, 
        ResolveVectorDelegate resolveVector)
    {
        var ret = new List<ValidFilter>();
        foreach (var (key, value) in dictionary)
        {
            if (key == SortConstant.SortKey)
            {
                continue;
            }

            var (_,_,filter, errors) = await Parse(entity, key, value, resolveVector);
            if (errors is not null)
            {
                return Result.Fail(errors);
            }

            ret.Add(filter);

        }

        return ret.ToImmutableArray();
    }

    private static async Task<Result<ValidFilter>> Parse(LoadedEntity entity, string field, Dictionary<string,StringValues> dictionary, 
        ResolveVectorDelegate resolveVector)
    {
        var (_,_, vector, errors)  = await resolveVector(entity, field);
        if (errors is not null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}, errors: {errors}");
        }

        var op = dictionary.TryGetValue("operator", out var value) ? value.ToString() : "and";
        var constraints = dictionary
            .Where(x => x.Key != "operator")
            .Select(x => new ValidConstraint(x.Key,[vector.Attribute.Cast(x.Value.ToString())]));
        return new ValidFilter(vector,op, [..constraints]);
    }
}