using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public static class MongoExt
{
    public static SortDefinition<T>? GetSortDefinition<T>(Sorts sorts)
    {
        var sortBuilder = Builders<T>.Sort;
        if (sorts.Count == 0) return null;
        
        var ret = sorts[0].Order == SortOrder.Asc
                                  ? sortBuilder.Ascending(sorts[0].FieldName)
                                  : sortBuilder.Descending(sorts[0].FieldName);
        
        for (var i = 1; i < sorts.Count; i++)
        {
            ret = sorts[i].Order == SortOrder.Asc
                ? ret.Ascending(sorts[i].FieldName)
                : ret.Descending(sorts[i].FieldName);
        }
        return ret;
    }

    public static Result<FilterDefinition<BsonDocument>> GetCursorFilters(Cursor cursor, Sorts sorts)
    {
        var builder = Builders<BsonDocument>.Filter;
        if (cursor.BoundaryItem is null) return builder.Empty;
        
        return sorts.Count switch
        {
            0 => Result.Fail("Sorts was not provided, can not perform cursor filter"),
            1 => GetCompare(sorts[0]),
            2 => builder.Or(GetCompare(sorts[0]), builder.And(GetEq(sorts[0]), GetCompare(sorts[1]))),
            _ => Result.Fail("More than two field in sorts is not supported"),
        };

        FilterDefinition<BsonDocument> GetEq(Sort sort)
        {
            var (fld, val) = (sort.FieldName, cursor.BoundaryValue(sort.FieldName));
            return builder.Eq(fld, val);
        }
  
        FilterDefinition<BsonDocument> GetCompare(Sort sort)
        {
            var (fld, val) = (sort.FieldName, cursor.BoundaryValue(sort.FieldName));
            return cursor.GetCompareOperator(sort) == ">" ? builder.Gt(fld, val) : builder.Lt(fld, val);
        } 
    }
    public static Result<List<FilterDefinition<BsonDocument>>> GetFiltersDefinition(Filters? filters)
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

    private static Result<FilterDefinition<BsonDocument>> GetFilterDefinition(Filter filter)
    {
        var builder = Builders<BsonDocument>.Filter;
        List<FilterDefinition<BsonDocument>> definitions = new();
        foreach (var filterConstraint in filter.Constraints)
        {
            var resConstraint = GetConstraintDefinition(
                filter.FieldName, filterConstraint.Match, filterConstraint.ResolvedValues);
            if (resConstraint.IsFailed)
            {
                return Result.Fail(resConstraint.Errors);
            }
            definitions.Add(resConstraint.Value);
        }

        return filter.IsOr ? builder.Or(definitions) : builder.And(definitions);
    }

    private static Result<FilterDefinition<BsonDocument>> GetConstraintDefinition(string fieldName, string match, object[]values)
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