using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Utils.Graph;

public record GraphQlArgumentValueProvider(GraphQLArgument Argument) : IValueProvider
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public ValueWrapper Val()
    {
        return new ValueWrapper(Argument.Value switch
        {
            GraphQLStringValue stringValue => stringValue.ToString()!,
            GraphQLEnumValue enumValue => enumValue.Name.StringValue,
            GraphQLIntValue intValue => intValue.ToString()!,
            GraphQLObjectValue objectValue => objectValue.ToPairs() is var pairsResult && pairsResult.IsFailed
                ? pairsResult.Errors
                : pairsResult.Value,
            _ => new List<IError>{new Error("Unsupported value type")}
        });
    }
}

public record ArgumentKeyValueValueProvider(string Key, ArgumentValue Value) : IValueProvider
{
    public string Name()
    {
        return Key;
    }

    public ValueWrapper Val()
    {
        return new ValueWrapper( Value.Value switch
        {
            string stringValue => stringValue,
            int intValue => intValue.ToString(),
            object[] objects => objects.Select(x => x.ToString()!).ToImmutableArray(),
            _ => new List<IError>{new Error("Unsupported value type")}
        });
    }
}