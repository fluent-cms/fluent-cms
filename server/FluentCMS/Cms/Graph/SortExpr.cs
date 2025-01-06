using FluentCMS.Core.Descriptors;
using GraphQL.Types;

namespace FluentCMS.Cms.Graph;


public sealed class SortOrderEnum : EnumerationGraphType
{
    public SortOrderEnum()
    {
        Name = "SortOrderEnum";
        Add(new EnumValueDefinition(SortOrder.Asc, SortOrder.Asc));
        Add(new EnumValueDefinition(SortOrder.Desc, SortOrder.Desc));
    }
}

public sealed class SortExpr : InputObjectGraphType
{
    public SortExpr()
    {
        Name = SortConstant.SortExprKey;
        AddField(new FieldType
        {
            Name = SortConstant.FieldKey,
            Type = typeof(StringGraphType),
        });
        AddField(new FieldType
        {
            Name = SortConstant.OrderKey,
            Type = typeof(SortOrderEnum),
        });
    }
}