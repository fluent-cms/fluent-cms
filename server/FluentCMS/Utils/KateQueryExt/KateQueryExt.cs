using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.KateQueryExt;

public static class KateQueryExt
{
    public static void ApplyJoin(this SqlKata.Query query, IEnumerable<AttributeVector> vectors)
    {
        var root = AttributeTreeNode.Parse(vectors);
        bool hasCrosstable = false;
        Bfs(root, "");
        if (hasCrosstable)
        {
            query.Distinct();
        }

        void Bfs(AttributeTreeNode node, string prefix)
        {
            var nextPrefix = prefix;
            if (node.Attribute is not null)
            {
                if (nextPrefix != "")
                {
                    nextPrefix += AttributeVectorConstants.Separator;
                }
                nextPrefix += node.Attribute.Field;

                switch (node.Attribute.Type)
                {
                    case DisplayType.Lookup:
                        var lookup = node.Attribute.Lookup!;
                        query
                            .LeftJoin($"{lookup.TableName} as {nextPrefix}",
                            node.Attribute.AddTableModifier(prefix),
                            lookup.PrimaryKeyAttribute.AddTableModifier(nextPrefix))
                            .Where(lookup.DeletedAttribute.AddTableModifier(nextPrefix), false);
                        break;
                    case DisplayType.Crosstable:
                        hasCrosstable = true;
                        var cross = node.Attribute.Crosstable;
                        var crossAlias = $"{nextPrefix}_{cross!.CrossEntity.TableName}";
                        query
                            .LeftJoin($"{cross.CrossEntity.TableName} as {crossAlias}",
                                cross.SourceEntity.PrimaryKeyAttribute.AddTableModifier(prefix),
                                cross.SourceAttribute.AddTableModifier(crossAlias))
                            .LeftJoin($"{cross.TargetEntity.TableName} as {nextPrefix}",
                                cross.TargetAttribute.AddTableModifier(crossAlias),
                                cross.TargetEntity.PrimaryKeyAttribute.AddTableModifier(nextPrefix))
                            .Where(cross.CrossEntity.DeletedAttribute.AddTableModifier(crossAlias),false)
                            .Where(cross.TargetEntity.DeletedAttribute.AddTableModifier(nextPrefix),false);
                    break;
                }
            }

            foreach (var sub in node.Children)
            {
                Bfs(sub, nextPrefix);
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
            var vector = sort.Vector;
            if (sort.Order == SortOrder.Desc)
            {
                query.OrderByDesc(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
            else
            {
                query.OrderBy(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
        }
    }

    public static Result ApplyFilters(this SqlKata.Query query, IEnumerable<ValidFilter> filters)
    {
        var result = Result.Ok();
        foreach (var filter in filters)
        {
            var filedName = filter.Vector.Attribute.AddTableModifier(filter.Vector.TableAlias);
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

    private static Result<SqlKata.Query> ApplyAndConstraint(this SqlKata.Query query, string field, string match, ImmutableArray<object> values)
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
            Matches.Between => values.Length == 2
                ? query.WhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"{match} is not support ")
        };
    }

    private static Result<SqlKata.Query> ApplyOrConstraint(this SqlKata.Query query, string field, string match, ImmutableArray<object> values)
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
            Matches.Between => values.Length == 2
                ? query.OrWhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"{match} is not support ")
        };
    }

    
    public static void ApplyCursor(this SqlKata.Query? query,  ValidSpan? cursor,ImmutableArray<ValidSort> sorts)
    {
        if (query is null || cursor?.EdgeItem is null)
        {
            return;
        }

        query.Where(q =>
        {
            for (var i = 0; i < sorts.Length; i++)
            {
                ApplyFilter(q, i);
                if (i < sorts.Length - 1)
                {
                    q.Or();
                }
            }
            return q;
        });
        return ;

        void ApplyFilter(SqlKata.Query q,int idx)
        {
            for (var i = 0; i < idx; i++)
            {
                ApplyEq(q, sorts[i]);
            }
            ApplyCompare(q,sorts[idx]);
        }

        void ApplyEq(SqlKata.Query q, ValidSort sort)
        {
            q.Where(sort.Vector.Attribute.AddTableModifier(sort.Vector.TableAlias),  cursor.Edge(sort.Vector.FullPath));
        }

        void ApplyCompare(SqlKata.Query q, ValidSort sort)
        {
            q.Where(sort.Vector.Attribute.AddTableModifier(sort.Vector.TableAlias), cursor.Span.GetCompareOperator(sort.Order), cursor.Edge(sort.Vector.FullPath));
        }
        
    }
}