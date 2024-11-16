using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Graph;

public static class GraphTypeHelper
{
    public static void LoadCompoundGraphType(
        this Entity entity,
        Dictionary<string, ObjectGraphType> singleDict,
        Dictionary<string, ListGraphType> listDict)
    {
        var currentType = singleDict[entity.Name];
        foreach (var attribute in entity.Attributes)
        {
            var t = new FieldType
            {
                Name = attribute.Field,
                Resolver = Resolvers.ValueResolver,
                ResolvedType = attribute.Type switch
                {
                     DisplayType.Crosstable when attribute.GetCrosstableTarget(out var target)  => listDict[target], 
                     DisplayType.Lookup when attribute.GetLookupTarget(out var target) => singleDict[target],
                    _ => null 
                }
            };
            if (t.ResolvedType is not null)
            {
                currentType.AddField(t);
            }
        }
    }

    public static ObjectGraphType GetPlainGraphType(this Entity entity)
    {
        var entityType = new ObjectGraphType
        {
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entity.Name)
        };

        foreach (var attr in entity.Attributes.Where(x => !x.IsCompound()))
        {
            entityType.AddField(new FieldType
            {
                Name = attr.Field,
                ResolvedType = attr.GetPlainGraphType(),
                Resolver = Resolvers.ValueResolver
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