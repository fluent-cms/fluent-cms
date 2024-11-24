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
        args.AddRange(arr.Select(attr => attr.SimpleFilter()));
        args.AddRange(arr.Select(attr => attr.ComplexFilter()));
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

    private static QueryArgument ComplexFilter(this Attribute attr) => attr.DataType switch
    {
        DataType.Int => new QueryArgument<ListGraphType<IntClause>> { Name = attr.Field },
        DataType.Datetime => new QueryArgument<ListGraphType<DateClause>> { Name = attr.Field },
        _ => new QueryArgument<ListGraphType<StringClause>> { Name = attr.Field }
    };

    private static QueryArgument SimpleFilter(this Attribute attr) => attr.DataType switch
    {
        DataType.Int => new QueryArgument<ListGraphType<IntGraphType>>
            { Name = attr.Field + FilterConstants.SetSuffix },
        DataType.Datetime => new QueryArgument<ListGraphType<DateGraphType>>
            { Name = attr.Field + FilterConstants.SetSuffix },
        _ => new QueryArgument<ListGraphType<StringGraphType>> { Name = attr.Field + FilterConstants.SetSuffix }
    };

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
}