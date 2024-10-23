using System.Collections.Immutable;
using FluentCMS.Utils.Qs;
using FluentResults;
using Microsoft.Extensions.Primitives;
namespace FluentCMS.Utils.QueryBuilder;

public sealed record Filter(string FieldName, string Operator, ImmutableArray<Constraint> Constraints, bool OmitFail);
public sealed record ValidFilter(string FieldName, string Operator, ImmutableArray<ValidConstraint> Constraints);

public static class FilterHelper
{
    public static Result<ImmutableArray<ValidFilter>> Resolve(this IEnumerable<Filter> filters,string entityName, ImmutableArray<LoadedAttribute> attributes, Func<string, string, object> cast,  
        Dictionary<string, StringValues>? querystringDictionary)
    {
        var ret = new List<ValidFilter>();
        foreach (var filter in filters)
        {
            var attribute = attributes.FindOneAttribute(filter.FieldName);
            if (attribute is null)
            {
                return Result.Fail($"Fail to resolve filter: no field ${filter.FieldName} in ${entityName}");
            }    
            var res= filter.Constraints.Resolve(filter.OmitFail, entityName, attribute, querystringDictionary, cast);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            if (res.Value.Length > 0)
            {
                ret.Add(new ValidFilter(filter.FieldName,filter.Operator, res.Value));
            }
        }

        return ret.ToImmutableArray();
    }
    
    public static Result<ImmutableArray<ValidFilter>> Parse(LoadedEntity entity, QsDict qsDict, Func<string, string, object> cast)
    {
        var ret = new List<ValidFilter>();
        foreach (var pair in qsDict.Dict)
        {
            if (pair.Key == SortHelper.SortKey)
            {
                continue;
            }
            var result = Parse(entity, pair.Key, pair.Value.ToArray(), cast);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
            ret.Add(result.Value);
        }

        return ret.ToImmutableArray();
    }

    private static Result<ValidFilter> Parse(LoadedEntity entity, string field, Pair[] pairs, Func<string, string, object> cast)
    {
        var attribute = entity.Attributes.FindOneAttribute(field);
        if (attribute is null)
        {
            return Result.Fail($"Fail to parse filter, not found {entity.Name}.{field}");
        }

        var op = pairs.FirstOrDefault(x => x.Key == "operator")?.Values.FirstOrDefault() ?? "and";
        return new ValidFilter(field, op,
            (from pair in pairs.Where(x => x.Key != "operator")
                from pairValue in pair.Values
                select new ValidConstraint(pair.Key, [cast(attribute.Field, pairValue)])).ToImmutableArray());
    }
}