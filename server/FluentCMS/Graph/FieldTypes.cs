using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQL.Types;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
namespace FluentCMS.Graph;

public static class FieldTypes
{
    public static ObjectGraphType PlainType(Entity entity)
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
                ResolvedType = PlainGraphType(attr),
                Resolver = Resolvers.ValueResolver
            });
        }

        return entityType;
    }
    
    public static void SetCompoundType(Entity entity, Dictionary<string, GraphInfo> dict)
    {
        var current = dict[entity.Name].SingleType;
        foreach (var attribute in entity.Attributes.Where(x=>x.IsCompound()))
        {
            var t = new FieldType
            {
                Name = attribute.Field,
                Resolver = Resolvers.ValueResolver,
                ResolvedType = attribute.Type switch
                {
                    DisplayType.Crosstable when attribute.GetCrosstableTarget(out var target) => dict[target].ListType,
                    DisplayType.Lookup when attribute.GetLookupTarget(out var target) => dict[target].SingleType,
                    _ => null
                },
                Arguments = attribute.Type switch
                {
                    DisplayType.Crosstable when attribute.GetCrosstableTarget(out var target) => CrosstableARgs(target),
                    _ => null
                }
            };
            
            if (t.ResolvedType is not null)
            {
                current.AddField(t);
            }
        }

        return;

        QueryArguments CrosstableARgs(string target)
        {
            var find = dict[target].Entity;
            return new QueryArguments([
                Args.OffsetArg,
                Args.LimitArg,
                Args.SortArg(find),
                ..Args.FilterArgs(find)
            ]);
        }
    }

    private static IGraphType PlainGraphType( Attribute attribute)
    {
        return attribute.DataType switch
        {
            DataType.Int => new IntGraphType(),
            DataType.Datetime => new DateTimeGraphType(),
            _ => new StringGraphType()
        };
    }
    

}