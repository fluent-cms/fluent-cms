using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FluentCMS.Utils.Nosql;

public static class MongoFilterBuilder
{
    public static Result<FilterDefinition<BsonDocument>> GetFiltersDefinition(Filters? filters)
    {
        var builder = Builders<BsonDocument>.Filter;
        if (filters is null) return builder.Empty;

        List<FilterDefinition<BsonDocument>> definitions = new();
        foreach (var filter in filters)
        {
            var res = GetFilterDefinition(filter);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            definitions.Add(res.Value);
        }
        return  builder.And(definitions);
    } 
    
    public static Result<FilterDefinition<BsonDocument>> GetFilterDefinition(Filter filter)
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
    
    public static Result<FilterDefinition<BsonDocument>> GetConstraintDefinition(string fieldName, string match, object[]values)
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