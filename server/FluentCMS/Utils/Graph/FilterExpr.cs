using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;

public sealed class FilterExpr : InputObjectGraphType
{
    public FilterExpr()
    {
        Name = FilterConstants.FilterExprKey;
        AddField(new FieldType
        {
            Name = FilterConstants.FieldKey,
            Type = typeof(StringGraphType),
        });
        AddField(new FieldType
        {
            Name = FilterConstants.ClauseKey,
            Type = typeof(ListGraphType<Clause>)
        });
    }
}