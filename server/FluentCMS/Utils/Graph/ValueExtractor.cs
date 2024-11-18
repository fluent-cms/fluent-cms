using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL.Execution;
using GraphQLParser.AST;
using OneOf;

namespace FluentCMS.Utils.Graph;

public record GraphQlArgumentInput(GraphQLArgument Argument) : IInput
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public OneOf<string,ImmutableArray<(string,object)>,List<IError>> Val()
    {
        return Argument.Value switch
        {
            GraphQLStringValue stringValue => stringValue.ToString()!,
            GraphQLEnumValue enumValue => enumValue.Name.StringValue,
            GraphQLIntValue intValue => intValue.ToString()!,
            GraphQLObjectValue objectValue => objectValue.ToPairs() is var pairsResult && pairsResult.IsFailed
                ? pairsResult.Errors
                : pairsResult.Value,
            _ => new List<IError>{new Error("Unsupported value type")}
        };
    }
}

public record ArgumentKeyValueInput(string Key, ArgumentValue Value) : IInput
{
    public string Name()
    {
        return Key;
    }

    public OneOf<string,ImmutableArray<(string,object)>,List<IError>> Val()
    {
        return Value.Value switch
        {
            string stringValue => stringValue,
            int intValue => intValue.ToString(),
            _ => new List<IError>{new Error("Unsupported value type")}
        };
    }
}