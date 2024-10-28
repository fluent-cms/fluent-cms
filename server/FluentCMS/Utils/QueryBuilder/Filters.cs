using System.Collections.Immutable;
using FluentCMS.Utils.Qs;
using FluentResults;
using FluentResults.Extensions;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string Operator, ImmutableArray<Constraint> Constraints, bool OmitFail);

public sealed record ValidFilter(AttributeVector Vector, string Operator, ImmutableArray<ValidConstraint> Constraints);
   
public static class FilterHelper
{
    
    public static async Task<Result<ImmutableArray<ValidFilter>>> Resolve(
        this IEnumerable<Filter> filters,  
        LoadedEntity entity,
        Dictionary<string, StringValues>? querystringDictionary,
        ResolveVectorDelegate resolveVector 
        )
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            var (_, vectorFail, vector, vectorError) = await resolveVector(entity,filter.FieldName);
            if (vectorFail)
            {
                return Result.Fail(vectorError);
            }

            var (_, consFail, constraints, constraintErrors) = filter.Constraints.Resolve(vector.Attribute, filter.OmitFail,querystringDictionary);
            if (consFail)
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
    
    public static async Task<Result<ImmutableArray<ValidFilter>>> Parse(LoadedEntity entity, QsDict qsDict, ResolveVectorDelegate resolveVector)
    {
        var ret = new List<ValidFilter>();
        foreach (var pair in qsDict.Dict)
        {
            if (pair.Key == SortConstant.SortKey)
            {
                continue;
            }
            var result =await Parse(entity, pair.Key, pair.Value.ToArray(),resolveVector );
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
            ret.Add(result.Value);
        }

        return ret.ToImmutableArray();
    }

    private static async Task<Result<ValidFilter>> Parse(LoadedEntity entity, string field, Pair[] pairs, 
        ResolveVectorDelegate resolveVector)
    {
        var res  = await resolveVector(entity, field);
        if (res.IsFailed)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}");
        }

        var op = pairs.FirstOrDefault(x => x.Key == "operator")?.Values.FirstOrDefault() ?? "and";
        var constraints = from pair in pairs.Where(x => x.Key != "operator")
            from pairValue in pair.Values
            select new ValidConstraint(pair.Key, [res.Value.Attribute.Cast(pairValue)]);
        
        return new ValidFilter(res.Value,op, [..constraints]);
    }
}