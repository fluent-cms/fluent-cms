using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;
using FluentResults;
using GraphQL.Validation;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.Graph;

//convert graphQL type to common c# types
public static class Converter
{
    public static string[] GetRequiredNames(this Variables? variables)
    {
        if (variables == null)
        {
            return [];
        }
        
        var ret = new List<string>();
        foreach (var variable in variables)
        {
            if (variable.Definition.Type is GraphQLNonNullType)
            {
                ret.Add(variable.Name);
            }
        }
        return ret.ToArray();
    }
    public static StrArgs ToQueryStrArgs(this Variables? variables)
    {
        var dictionary = new Dictionary<string, StringValues>();
        if (variables is null)
        {
            return dictionary;
        }

        foreach (var variable in variables)
        {
            if (variable.Value is not null)
            {
                dictionary.Add(variable.Name, variable.Value?.ToString());
            }
        }

        return dictionary;
    }

    public static Result<GraphQLField[]> GetRootGraphQlFields(string s)
    {
        var document = GraphQLParser.Parser.Parse(s);
        var def = document.Definitions.FirstOrDefault();
        if (def is null)
        {
            return Result.Fail("can not find root ASTNode");
        }

        if (def is not GraphQLOperationDefinition op)
        {
            return Result.Fail("root ASTNode is not operation definition");
        }

        var sub = op.SelectionSet.SubFields();
        if (sub.Length == 1 && sub[0].SelectionSet is not null)
        {
            return sub[0].SelectionSet!.SubFields();
        }
        return op.SelectionSet.SubFields();
    }

    public static GraphQLField[] SubFields(this GraphQLSelectionSet selectionSet)
    {
        return [..selectionSet.Selections.OfType<GraphQLField>()];
    }

    public static string[] ToPrimeStrings(this GraphQLListValue vals)
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
            GraphQLStringValue stringValue => stringValue.Value.ToString(),
            GraphQLEnumValue enumValue => enumValue.Name.StringValue,
            GraphQLVariable graphQlVariable => QueryConstants.VariablePrefix + graphQlVariable.Name.StringValue,
            _ => ""
        };
        return !string.IsNullOrEmpty(result);
    }

    public static StrPair[] ToPairs(this GraphQLValue? v)
    {
        return v is null ? []: v switch
        {
            GraphQLObjectValue objectValue => objectValue.ToPairs(),
            GraphQLListValue listValue => listValue.ToPairs(),
            _ => []
        };
    }

    private static StrPair[] ToPairs(this GraphQLListValue pairs)
    {
        var ret = new List<StrPair>();
        foreach (var val in pairs.Values??[])
        {
            var list = val switch
            {
                GraphQLObjectValue obj => obj.ToPairs(),
                _ => []
            };
            ret.AddRange(list);
        }
        return [..ret];
    }

    private static StrPair[] ToPairs(this GraphQLObjectValue objectValue)
    {
        var result = new List<StrPair>();
        foreach (var field in objectValue.Fields ?? [])
        {
            var arr = field.Value switch
            {
                GraphQLListValue ls => ls.Values?
                    .Select(x=>x.ToPrimitiveString(out var v)?v:"")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray(),
                _ =>  field.Value.ToPrimitiveString(out var v)?[v]:[]   
            };
            result.Add(new StrPair(field.Name.StringValue, arr??[]));
        }
        return [..result];
    }
}