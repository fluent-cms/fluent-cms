using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQLParser;
using GraphQLParser.AST;

namespace FluentCMS.Utils.GraphQlExt;

public static class GraphQlExt
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
}