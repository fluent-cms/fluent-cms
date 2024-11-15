using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
using LongGraphType = GraphQL.Server.Types.LongGraphType;

namespace FluentCMS.Utils.Graph;

public static class EntityTypeHelper
{
    public static ObjectGraphType EntityGraphType(this LoadedEntity entity)
    {
        var entityType = new ObjectGraphType
        {
            Name = entity.Name
        };

        foreach (var attr in entity.Attributes.GetLocalAttrs())
        {
            entityType.AddField(new FieldType
            {
                Name = attr.Field,
                ResolvedType = attr.GetGraphType()
            });
        }
        return entityType;
    }

    private static IGraphType GetGraphType(this Attribute attribute)
    {
        return attribute.DataType switch
        {
            DataType.Int => new IntGraphType(),
            DataType.Datetime => new DateTimeGraphType(),
            _ => new StringGraphType()
        };
    }
}