using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Graph;

public static class ArgumentTypes
{
    public static QueryArgument[] FilterArgs(this Entity entity)
    {
        var args = new List<QueryArgument>();
        var arr = entity.Attributes.GetLocalAttrs();
        args.AddRange(arr.Select(SimpleFilter));
        args.AddRange(arr.Select(ComplexFilter));
        return args.ToArray();
    }

    public static QueryArgument SortArg(this Entity entity) =>
        new(new ListGraphType(entity.GetSortFieldEnumGraphType()))
        {
            Name = SortConstant.SortKey
        };


    public static QueryArgument SortExpr() => new QueryArgument<ListGraphType<SortExpr>>
    {
        Name = SortConstant.SortExprKey
    };

    public static QueryArgument FilterExpr() => new QueryArgument<ListGraphType<FilterExpr>>
    {
        Name = FilterConstants.FilterExprKey
    };

    private static QueryArgument ComplexFilter( Attribute attr) => attr.DataType switch
    {
        DataType.Int => new QueryArgument<ListGraphType<IntClause>> { Name = attr.Field },
        DataType.Datetime => new QueryArgument<ListGraphType<DateClause>> { Name = attr.Field },
        _ => new QueryArgument<ListGraphType<StringClause>> { Name = attr.Field }
    };

    private static QueryArgument SimpleFilter(Attribute attr)
    {
        var arg = attr.DataType switch
        {
            DataType.Int => new QueryArgument<ListGraphType<IntGraphType>>(),
            DataType.Datetime => new QueryArgument<ListGraphType<DateGraphType>>(),
            _ => attr.Type switch
            {
                DisplayType.Dropdown => new QueryArgument(new ListGraphType(GetAttributeOptionEnum(attr))),
                DisplayType.Multiselect => new QueryArgument(new ListGraphType(GetAttributeOptionEnum(attr))),
                _ => new QueryArgument<ListGraphType<StringGraphType>>()
            }
        };
        
        arg.Name = attr.Field + FilterConstants.SetSuffix;
        return arg;
    }

    private static EnumerationGraphType GetSortFieldEnumGraphType(this Entity entity)
    {
        var type = new EnumerationGraphType
        {
            Name = "SortFields"
        };
        var arr = entity.Attributes.GetLocalAttrs();
        foreach (var attribute in arr)
        {
            type.Add(new EnumValueDefinition(attribute.Field, attribute.Field));
        }

        foreach (var attribute in arr)
        {
            type.Add(new EnumValueDefinition(attribute.Field + SortOrder.Desc, attribute.Field + SortOrder.Desc));
        }

        return type;
    }

    private static EnumerationGraphType GetAttributeOptionEnum(Attribute attribute)
    {
        var type = new EnumerationGraphType
        {
            Name = attribute.Field + "Enum"
        };
        foreach (var selectItem in attribute.GetSelectItems(out var selectItems) ? selectItems : [])
        {
            type.Add(new EnumValueDefinition(selectItem, selectItem));
        }

        return type;
    }
}