using System.Globalization;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Graph;

public static class Args
{
    public static QueryArgument[] FilterArgs(Entity entity)
    {
        var args = new List<QueryArgument>();
        var arr = entity.Attributes.GetLocalAttrs();
        args.AddRange(arr.Select(SimpleFilter));
        args.AddRange(arr.Select(ComplexFilter));
        return args.ToArray();
    }

    public static QueryArgument SortArg(Entity entity) =>
        new(new ListGraphType(GetSortFieldEnumGraphType(entity)))
        {
            Name = SortConstant.SortKey
        };


    private static QueryArgument ComplexFilter(Attribute attr)
    {
        QueryArgument arg = attr.DataType switch
        {
            DataType.Int => IntClauseArg(attr.Field),
            DataType.Datetime => DateClauseArg(attr.Field),
            _ => StringClauseArg(attr.Field)
        };
        return arg;
    }


    private static QueryArgument SimpleFilter(Attribute attr)
    {
        var arg = attr.DataType switch
        {
            DataType.Int => new QueryArgument<ListGraphType<IntGraphType>>(),
            DataType.Datetime => new QueryArgument<ListGraphType<DateGraphType>>(),
            _ => attr.DataType switch
            {
                DataType.Lookup => new QueryArgument(new ListGraphType(GetAttributeOptionEnum(attr))),
                _ => new QueryArgument<ListGraphType<StringGraphType>>()
            }
        };

        arg.Name = attr.Field + FilterConstants.SetSuffix;
        return arg;
    }

    private static EnumerationGraphType GetSortFieldEnumGraphType(Entity entity)
    {
        var type = new EnumerationGraphType
        {
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name) + "SortEnum"
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

    //have to create a new arguments for each field
    private static QueryArgument StringClauseArg(string name) => new QueryArgument<ListGraphType<StringClause>>
    {
        Name = name
    };

    private static QueryArgument DateClauseArg(string name) => new QueryArgument<ListGraphType<DateClause>>
    {
        Name = name
    };

    private static QueryArgument IntClauseArg(string name) => new QueryArgument<ListGraphType<IntClause>>
    {
        Name = name
    };
    
    public static  QueryArgument LimitArg => new QueryArgument<IntGraphType>
    {
        Name = PaginationConstants.LimitKey
    };

    public static  QueryArgument OffsetArg => new QueryArgument<IntGraphType>
    {
        Name = PaginationConstants.OffsetKey
    };

    public static QueryArgument SortExprArg => new QueryArgument<ListGraphType<SortExpr>>
    {
        Name = SortConstant.SortExprKey
    };

    public static  QueryArgument FilterExprArg => new  QueryArgument<ListGraphType<FilterExpr>>
    {
        Name = FilterConstants.FilterExprKey
    };
}






