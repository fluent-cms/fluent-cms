using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Graph;


public static class ArgumentTypes
{
    public static QueryArguments GetArgument(this Entity entity, bool needSort)
    {
        var args = new List<QueryArgument>();
        if (needSort)
        {
            args.Add(entity.GetSortArgument());
        }
        args.AddRange(entity.Attributes
            .Where(x => !x.IsCompound())
            .Select(attr => attr.GetSimpleFilterArgument()));
        
        args.AddRange(entity.Attributes
            .Where(x => !x.IsCompound())
            .Select(attr => attr.GetFilterArgument()));
        return new QueryArguments(args);
    }

    private static QueryArgument GetFilterArgument(this Attribute attr)
    {
        return attr.DataType switch
        {
            DataType.Int => new QueryArgument<ListGraphType<IntClause>> { Name = attr.Field},
            DataType.Datetime => new QueryArgument<ListGraphType<DateClause>> { Name = attr.Field },
            _ => new QueryArgument<ListGraphType<StrClause>> { Name = attr.Field }
        };
    }

    private static QueryArgument GetSimpleFilterArgument(this Attribute attr)
    {
        return attr.DataType switch
        {
            DataType.Int =>new QueryArgument<ListGraphType<IntGraphType>> { Name = attr.Field + FilterConstants.SetSuffix},
            DataType.Datetime =>new QueryArgument<ListGraphType<DateGraphType>> { Name = attr.Field + FilterConstants.SetSuffix},
            _ => new QueryArgument<ListGraphType<StringGraphType>> { Name = attr.Field + FilterConstants.SetSuffix}
        };
    }
    
    private static QueryArgument GetSortArgument(this Entity entity)
    {
        return new QueryArgument(new ListGraphType(entity.GetFieldEnumGraphType()))
        {
            Name = SortConstant.SortKey
        };
    }

    private static EnumerationGraphType GetFieldEnumGraphType(this Entity entity)
    {
        var type = new EnumerationGraphType
        {
            Name = entity.Name + "FieldEnum"
        };
        foreach (var attribute in entity.Attributes.Where(x => !x.IsCompound()))
        {
            type.Add(new EnumValueDefinition(attribute.Field, attribute.Field));
            type.Add(new EnumValueDefinition(attribute.Field + SortOrder.Desc  , attribute.Field + SortOrder.Desc));
        }

        return type;
    }
}