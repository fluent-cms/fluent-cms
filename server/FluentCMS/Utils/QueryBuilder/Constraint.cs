using FluentResults;
using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;



public sealed class Constraint
{
    public string Match { get; set; } = "";
    public string Value { get; set; } = "";

    public object[]? ResolvedValues { get; set; }

    private object GetValue()
    {
        return ResolvedValues?.FirstOrDefault() ?? Value;
    }
    public Result<Query> Apply(Query query, string field, bool or)
    {
        if (or)
            return Match switch
            {
                Matches.StartsWith => query.OrWhereStarts(field, GetValue()),
                Matches.Contains => query.OrWhereContains(field, GetValue()),
                Matches.NotContains => query.OrWhereNotContains(field, GetValue()),
                Matches.EndsWith => query.OrWhereEnds(field, GetValue()),
                Matches.EqualsTo => query.OrWhere(field, GetValue()),
                Matches.NotEquals => query.OrWhereNot(field, GetValue()),
                Matches.NotIn => query.OrWhereNotIn(field, ResolvedValues),
                Matches.In => query.OrWhereIn(field, ResolvedValues),
                Matches.Lt => query.OrWhere(field, "<", GetValue()),
                Matches.Lte => query.OrWhere(field, "<=", GetValue()),
                Matches.Gt => query.OrWhere(field, ">", GetValue()),
                Matches.Gte => query.OrWhere(field, ">=", GetValue()),
                Matches.DateIs => query.OrWhereDate(field, GetValue()),
                Matches.DateIsNot => query.OrWhereNotDate(field, GetValue()),
                Matches.DateBefore => query.OrWhereDate(field, "<", GetValue()),
                Matches.DateAfter => query.OrWhereDate(field, ">", GetValue()),
                Matches.Between => ResolvedValues?.Length == 2
                    ? query.OrWhereBetween(field, ResolvedValues[0], ResolvedValues[1])
                    : Result.Fail("show provide two values for between"),
                _ => Result.Fail($"{Match} is not support ")
            };
        return Match switch
        {
            Matches.StartsWith => query.WhereStarts(field, GetValue()),
            Matches.Contains => query.WhereContains(field, GetValue()),
            Matches.NotContains => query.WhereNotContains(field, GetValue()),
            Matches.EndsWith => query.WhereEnds(field, GetValue()),
            Matches.EqualsTo => query.Where(field, GetValue()),
            Matches.NotEquals => query.WhereNot(field, GetValue()),
            Matches.NotIn => query.WhereNotIn(field, ResolvedValues),
            Matches.In => query.WhereIn(field, ResolvedValues),
            Matches.Lt => query.Where(field, "<", GetValue()),
            Matches.Lte => query.Where(field, "<=", GetValue()),
            Matches.Gt => query.Where(field, ">", GetValue()),
            Matches.Gte => query.Where(field, ">=", GetValue()),
            Matches.DateIs => query.WhereDate(field, GetValue()),
            Matches.DateIsNot => query.WhereNotDate(field, GetValue()),
            Matches.DateBefore => query.WhereDate(field, "<", GetValue()),
            Matches.DateAfter => query.WhereDate(field, ">", GetValue()),
            Matches.Between => ResolvedValues?.Length == 2
                ? query.WhereBetween(field, ResolvedValues[0], ResolvedValues[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"{Match} is not support ")
        };
    }
}

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
}