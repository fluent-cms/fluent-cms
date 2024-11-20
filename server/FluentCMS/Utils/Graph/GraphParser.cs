using System.Collections.Immutable;
using GraphQLParser;
using GraphQLParser.AST;
using FluentResults;

namespace FluentCMS.Utils.Graph;

public static class GraphParser
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

    public static ImmutableArray<string> ToPrimeStrings(this GraphQLListValue vals)
    {
        var ret = new List<string>();
        foreach (var graphQlValue in vals.Values??[])
        {
            if (!graphQlValue.ToPrimitiveString(out var s))
            {
                return [];
            }
            ret.Add(s);
        }

        return [..ret];
    }
    public static bool ToPrimitiveString(this GraphQLValue value, out string result)
    {
        result = value switch
        {
            GraphQLIntValue integer => integer.Value.ToString(),
            GraphQLStringValue stringValue => stringValue.ToString()!,
            GraphQLEnumValue enumValue => enumValue.Name.StringValue,
            _ => ""
        };
        return !string.IsNullOrEmpty(result);
    }
    
    public static ImmutableArray<(string,object)> ToPairs(this GraphQLListValue vals)
    {
        var ret = new List<(string,object)>();
        foreach (var graphQlValue in vals.Values??[])
        {
            if (graphQlValue is not GraphQLObjectValue objectValue)
            {
                return [];
            }

            foreach (var (k, o) in objectValue.ToPairs())
            {
                ret.Add((k,o));
            }
        }
        return [..ret];
    }
    
    public static ImmutableArray<(string, object)> ToPairs(this GraphQLObjectValue objectValue)
    {
        var result = new List<(string, object)>();
        foreach (var field in objectValue.Fields ?? [])
        {
            if (!field.Value.ToPrimitiveString(out var v))
            {
                return [];
            }
            result.Add((field.Name.StringValue, v));
        }

        return [..result];
    }
}