using System.Collections.Immutable;
using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.Graph;

public static class GraphTypeHelper
{
    public static Result<ImmutableArray<GraphQLField>> GetRootGraphQlFields(string s)
    {
        var document = Parser.Parse(s);
        var def = document.Definitions.FirstOrDefault();
        if (def is null)
        {
            return Result.Fail("can not find root ASTNode");
        }

        if (def is not GraphQLOperationDefinition op)
        {
            return Result.Fail("root ASTNode is not operation definition");
        }

        return op.SelectionSet.SubFields();
    }

    public static ImmutableArray<GraphQLField> SubFields(this GraphQLSelectionSet selectionSet)
    {
        return [..selectionSet.Selections.OfType<GraphQLField>()];
    }

   

    public static Result<ImmutableArray<(string, object)>> ToPairs(this GraphQLObjectValue objectValue)
    {
        var result = new List<(string, object)>();
        foreach (var field in objectValue.Fields ?? [])
        {
            var (_, _, v, e) = field.Value.ToPrimitive();
            if (e != null)
            {
                return Result.Fail([new Error($"fail to resolve value {field.Name}"), ..e]);
            }

            result.Add((field.Name.StringValue, v));
        }

        return result.ToImmutableArray();
    }

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
                    DisplayType.Crosstable when attribute.GetCrosstableTarget(out var target) => listDict[target],
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

    public static QueryArguments GetArgument(this Entity entity,bool needSort)
    {
        var args = new List<QueryArgument>();
        if (needSort) args.Add(entity.GetSortArgument());
        args.AddRange(entity.Attributes.Where(x => !x.IsCompound()).Select(attr => attr.GetFilterArgument()));
        return new QueryArguments(args);
    }

    private static QueryArgument GetFilterArgument(this Attribute attr)
    {
        return attr.DataType switch
        {
            DataType.Int => new QueryArgument<IntGraphType> { Name = attr.Field },
            DataType.Datetime => new QueryArgument<DateTimeGraphType> { Name = attr.Field },
            _ => new QueryArgument<StringGraphType> { Name = attr.Field }
        };
    }

    private static QueryArgument GetSortArgument(this Entity entity)
    {
        return new QueryArgument(entity.GetFieldEnumGraphType())
        {
            Name = "sort"
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
        }

        return type;
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
    private static Result<object> ToPrimitive(this GraphQLValue graphQlValue)
    {
        object val;
        switch (graphQlValue)
        {
            case GraphQLEnumValue enumValue:
                val = enumValue.Name.StringValue;
                break;
            case GraphQLBooleanValue booleanValue:
                val = booleanValue.Value;
                break;
            case GraphQLIntValue intValue:
                val = intValue.Value;
                break;
            case GraphQLStringValue stringValue:
                val = stringValue.Value;
                break;
            default:
                return Result.Fail($"failed to convert {graphQlValue} to primitive value");
        }

        return val;
    } 
}