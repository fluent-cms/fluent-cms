using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public static class MongoExt
{
    public static SortDefinition<T>? GetSortDefinition<T>(ImmutableArray<ValidSort> sorts)
    {
        var sortBuilder = Builders<T>.Sort;
        if (sorts.Length == 0) return null;
        
        
        var ret = sorts[0].Order == SortOrder.Asc
                                  ? sortBuilder.Ascending(sorts[0].Vector.FullPath)
                                  : sortBuilder.Descending(sorts[0].Vector.FullPath);
        
        for (var i = 1; i < sorts.Length; i++)
        {
            ret = sorts[i].Order == SortOrder.Asc
                ? ret.Ascending(sorts[i].Vector.FullPath)
                : ret.Descending(sorts[i].Vector.FullPath);
        }
        return ret;
    }

    public static Result<FilterDefinition<BsonDocument>> GetCursorFilters(ValidSpan span, ImmutableArray<ValidSort> sorts)
    {
        var builder = Builders<BsonDocument>.Filter;
        if (span.EdgeItem?.Count == 0) return builder.Empty;
        
        var definitions = new List<FilterDefinition<BsonDocument>>();
        for (var i = 0; i < sorts.Length; i++)
        {
            definitions.Add(GetFilter(i));
        }
        return builder.Or(definitions);
        FilterDefinition<BsonDocument> GetFilter(int idx)
        {
            var list = new List<FilterDefinition<BsonDocument>>();
            for (var i = 0; i < idx; i++)
            {
                list.Add(GetEq(sorts[i]));
            } 
            list.Add(GetCompare(sorts[idx]));
            return builder.And(list);
        }
        
        FilterDefinition<BsonDocument> GetEq(ValidSort sort)
        {
            var (f, v) = (sort.Vector.FullPath, span.Edge(sort.Vector.FullPath).Value);
            return builder.Eq(f, v);
        }
  
        FilterDefinition<BsonDocument> GetCompare(ValidSort sort)
        {
            var (f, v) = (sort.Vector.FullPath, span.Edge(sort.Vector.FullPath).Value);
            return span.Span.GetCompareOperator(sort.Order) == ">" ? builder.Gt(f, v) : builder.Lt(f, v);
        } 
    }
    public static Result<List<FilterDefinition<BsonDocument>>> GetFiltersDefinition(IEnumerable<ValidFilter> filters)
    {
        var builder = Builders<BsonDocument>.Filter;
        List<FilterDefinition<BsonDocument>> definitions = new();
        foreach (var filter in filters??[])
        {
            var res = GetFilterDefinition(filter);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            definitions.Add(res.Value);
        }
        return  definitions;
    }

    private static Result<FilterDefinition<BsonDocument>> GetFilterDefinition(ValidFilter filter)
    {
        var builder = Builders<BsonDocument>.Filter;
        List<FilterDefinition<BsonDocument>> definitions = new();
        foreach (var filterConstraint in filter.Constraints)
        {
            var resConstraint = GetConstraintDefinition(filter.Vector.FullPath, filterConstraint.Match, [..filterConstraint.Values.GetValues()]);
            if (resConstraint.IsFailed)
            {
                return Result.Fail(resConstraint.Errors);
            }
            definitions.Add(resConstraint.Value);
        }

        return filter.MatchType=="or" ? builder.Or(definitions) : builder.And(definitions);
    }

    private static Result<FilterDefinition<BsonDocument>> GetConstraintDefinition(string fieldName, string match, ImmutableArray<object> values)
    {
        var builder = Builders<BsonDocument>.Filter;
        return match switch
        {
            Matches.Between => values.Length == 2
                ? builder.Gte(fieldName, values[0]) & builder.Lte(fieldName, values[1])
                : throw new Exception(),

            Matches.StartsWith => builder.Regex(fieldName, new BsonRegularExpression($"^{values[0]}")),

            Matches.Contains => builder.Regex(fieldName, new BsonRegularExpression((string)values[0], "i")),

            Matches.NotContains => builder.Not(builder.Regex("fieldName",
                new BsonRegularExpression((string)values[0], "i"))),

            Matches.EndsWith => builder.Regex(fieldName, new BsonRegularExpression($"{values[0]}$")),

            Matches.EqualsTo => builder.Eq(fieldName, values[0]),

            Matches.NotEquals => builder.Ne(fieldName, values[0]),

            Matches.In => builder.In(fieldName, values),

            Matches.NotIn => builder.Nin(fieldName, values),

            Matches.Lt => builder.Lt(fieldName, values[0]),

            Matches.Lte => builder.Lte(fieldName, values[0]),

            Matches.Gt => builder.Gt(fieldName, values[0]),

            Matches.Gte => builder.Gte(fieldName, values[0]),

            Matches.DateIs => builder.Eq(fieldName, values[0]),

            Matches.DateIsNot => builder.Ne(fieldName, values[0]),

            Matches.DateBefore => builder.Lt(fieldName, values[0]),

            Matches.DateAfter => builder.Gt(fieldName, values[0]),
            _ => Result.Fail($"Unknown match {match}")
        };
    } 
}