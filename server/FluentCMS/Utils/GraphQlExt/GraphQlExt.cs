using System.Collections.Immutable;
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
}