using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Resolvers;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Graph;

public static class EntityTypeHelper
{
    public static void LoadCompoundGraphType(this Entity entity, Dictionary<string, ObjectGraphType> dictionary)
    {
        var currentType = dictionary[entity.Name];
        foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Lookup))
        {
            if (attribute.GetCrosstableTarget(out var target))
            {
                currentType.AddField(new FieldType
                {
                    Name = attribute.Field,
                    ResolvedType = dictionary[target]
                });
            }
        }

        foreach (var attribute in entity.Attributes.GetAttrByType(DisplayType.Crosstable))
        {
            if (attribute.GetCrosstableTarget(out var target))
            {
                currentType.AddField(new FieldType
                {
                    Name = attribute.Field,
                    ResolvedType = dictionary[target]
                });
            }
        }
    }

    public static ObjectGraphType GetPlainGraphType(this Entity entity)
    {
        var entityType = new ObjectGraphType
        {
            Name =CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name)
        };

        foreach (var attr in entity.Attributes.Where(
                     x=> x.Type != DisplayType.Crosstable && x.Type != DisplayType.Lookup))
        {
            entityType.AddField(new FieldType
            {
                Name = attr.Field,
                ResolvedType = attr.GetPlainGraphType(),
                Resolver = new FuncFieldResolver<object>(context =>
                {
                    if (context.Source is Record record)
                    {
                        return record[attr.Field];
                    }

                    return null;
                })
            });
        }

        return entityType;
    }
    
    private static IGraphType GetPlainGraphType(this Attribute attribute)
    {
        return attribute.DataType switch
        {
            DataType.Int => new IntGraphType(),
            DataType.Datetime => new DateTimeGraphType(),
            _ => new StringGraphType()
        };
    }
}