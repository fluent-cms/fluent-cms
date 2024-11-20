using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;

namespace FluentCMS.Utils.Graph;


public sealed class SortOrderEnum : EnumerationGraphType
{
    public SortOrderEnum()
    {
        Name = "SortOrder";
        Add(new EnumValueDefinition(SortOrder.Asc, SortOrder.Asc));
        Add(new EnumValueDefinition(SortOrder.Desc, SortOrder.Desc));
    }
}

public sealed class SortExpr : InputObjectGraphType
{
    public SortExpr()
    {
        Name = "SortExpr";
        AddField(new FieldType
        {
            Name = "field",
            Type = typeof(StringGraphType),
        });
        AddField(new FieldType
        {
            Name = "order",
            Type = typeof(SortOrderEnum),
        });
    }
}