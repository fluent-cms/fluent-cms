using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = System.Attribute;

namespace FluentCMS.Utils.KateQueryExt;

public static class KateQueryExt
{
    public static void ApplyJoin(this SqlKata.Query query, IEnumerable<AttributeVector> vectors)
    {
        var lst = new List<AttributeVector>();
        var set = new HashSet<string>();
        foreach (var vector in vectors)
        {
            if (set.Contains(vector.FullPath))
            {
                continue;
            }
            lst.Add(vector);
            set.Add(vector.FullPath);
        }
        
        foreach (var attribute in lst.SelectMany(arr => arr.Attributes))
        {
            switch (attribute.Type)
            {
                case DisplayType.Lookup:
                    query.Join(attribute.Lookup!.TableName, attribute.GetFullName(),
                        attribute.Lookup.PrimaryKeyAttribute.GetFullName());
                    break;
            }
        }
    }

    public static void ApplyPagination(this SqlKata.Query query, ValidPagination pagination)
    {
        query.Offset(pagination.Offset).Limit(pagination.Limit);
    }
    public static void ApplySorts(this SqlKata.Query query, IEnumerable<ValidSort> sorts)
    {
        foreach (var sort in sorts)
        {
       
            if (sort.Order == SortOrder.Desc)
            {
                query.OrderByDesc(sort.Attributes.Last().GetFullName());
            }
            else
            {
                query.OrderBy(sort.Attributes.Last().GetFullName());
            }
        }
    }

    public static Result ApplyFilters(this SqlKata.Query query, IEnumerable<ValidFilter> filters)
    {
        var result = Result.Ok();
        foreach (var filter in filters)
        {
            var filedName = filter.Attributes.Last().GetFullName();
            query.Where(q =>
            {
                foreach (var c in filter.Constraints)
                {
                    var ret = filter.Operator == "or"
                        ? q.ApplyOrConstraint(filedName, c.Match, c.Values)
                        : q.ApplyAndConstraint(filedName, c.Match, c.Values);
                    if (ret.IsFailed)
                    {
                        result = Result.Fail(ret.Errors);
                        break;
                    }
                }

                return q;
            });
        }

        return result;
    }

    private static Result<SqlKata.Query> ApplyAndConstraint(this SqlKata.Query query, string field, string match, object[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.WhereStarts(field, values[0]),
            Matches.Contains => query.WhereContains(field, values[0]),
            Matches.NotContains => query.WhereNotContains(field, values[0]),
            Matches.EndsWith => query.WhereEnds(field, values[0]),
            Matches.EqualsTo => query.Where(field, values[0]),
            Matches.NotEquals => query.WhereNot(field, values[0]),
            Matches.NotIn => query.WhereNotIn(field, values),
            Matches.In => query.WhereIn(field, values),
            Matches.Lt => query.Where(field, "<", values[0]),
            Matches.Lte => query.Where(field, "<=", values[0]),
            Matches.Gt => query.Where(field, ">", values[0]),
            Matches.Gte => query.Where(field, ">=", values[0]),
            Matches.DateIs => query.WhereDate(field, values[0]),
            Matches.DateIsNot => query.WhereNotDate(field, values[0]),
            Matches.DateBefore => query.WhereDate(field, "<", values[0]),
            Matches.DateAfter => query.WhereDate(field, ">", values[0]),
            Matches.Between => values?.Length == 2
                ? query.WhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"{match} is not support ")
        };
    }

    private static Result<SqlKata.Query> ApplyOrConstraint(this SqlKata.Query query, string field, string match, object[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.OrWhereStarts(field, values[0]),
            Matches.Contains => query.OrWhereContains(field, values[0]),
            Matches.NotContains => query.OrWhereNotContains(field, values[0]),
            Matches.EndsWith => query.OrWhereEnds(field, values[0]),
            Matches.EqualsTo => query.OrWhere(field, values[0]),
            Matches.NotEquals => query.OrWhereNot(field, values[0]),
            Matches.NotIn => query.OrWhereNotIn(field, values),
            Matches.In => query.OrWhereIn(field, values),
            Matches.Lt => query.OrWhere(field, "<", values[0]),
            Matches.Lte => query.OrWhere(field, "<=", values[0]),
            Matches.Gt => query.OrWhere(field, ">", values[0]),
            Matches.Gte => query.OrWhere(field, ">=", values[0]),
            Matches.DateIs => query.OrWhereDate(field, values[0]),
            Matches.DateIsNot => query.OrWhereNotDate(field, values[0]),
            Matches.DateBefore => query.OrWhereDate(field, "<", values[0]),
            Matches.DateAfter => query.OrWhereDate(field, ">", values[0]),
            Matches.Between => values?.Length == 2
                ? query.OrWhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"{match} is not support ")
        };
    }

    
    public static Result ApplyCursor(this SqlKata.Query? query,  ValidCursor? cursor,ImmutableArray<ValidSort> sorts)
    {
        if (query is null || cursor?.BoundaryItem is null)
        {
            return Result.Ok();
        }

        var res = Result.Ok();
        query.Where(q =>
        {
            for (var i = 0; i < sorts.Length; i++)
            {
                res =ApplyFilter(q, i);
                if (res.IsFailed)
                {
                    break;
                }
                if (i < sorts.Length - 1)
                {
                    q.Or();
                }
            }
            return q;
        });
        return res;

        Result ApplyFilter(SqlKata.Query q,int idx)
        {
            for (var i = 0; i < idx; i++)
            {
                var res = ApplyEq(q, sorts[i]);
                if (res.IsFailed)
                {
                    return Result.Fail(res.Errors);
                }
            }
            var r = ApplyCompare(q,sorts[idx]);
            return r.IsFailed ? Result.Fail(r.Errors) : Result.Ok();
        }

        Result ApplyEq(SqlKata.Query q, ValidSort sort)
        {
            var res = cursor.BoundaryValue(sort.FullPath);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            q.Where(sort.Attributes.Last().GetFullName(), res.Value);
            return Result.Ok();
        }

        Result ApplyCompare(SqlKata.Query q, ValidSort sort)
        {
            var res = cursor.BoundaryValue(sort.FullPath);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            q.Where(sort.Attributes.Last().GetFullName(), cursor.Cursor.GetCompareOperator(sort.Order),res.Value );
            return Result.Ok();
        }
        
    }
}