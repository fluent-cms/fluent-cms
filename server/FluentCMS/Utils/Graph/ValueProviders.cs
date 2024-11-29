using FluentCMS.Utils.QueryBuilder;
using GraphQLParser.AST;

namespace FluentCMS.Utils.Graph;

public record GraphQlArgumentDataProvider(GraphQLArgument Argument) : IDataProvider
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public bool TryGetVals(out  string[] array)
    {
        array = Argument.Value switch
        {
            GraphQLListValue listValue => listValue.ToPrimeStrings(),
            _ => Argument.Value.ToPrimitiveString(out var s) ? [s]:[]  
        };
        return array.Length > 0;
    }

    public bool TryGetVal(out string? value)
    {
        return Argument.Value.ToPrimitiveString(out value);
    }

    public bool TryGetPairs(out StrPair[] pairs)
    {
        pairs = Argument.Value.ToPairs();
        return pairs.Length > 0;
    }

    public bool TryGetNodes(out IFieldNode[] nodes)
    {
        var objectValueList = Argument.Value switch
        {
            GraphQLListValue listValue => listValue.Values?
                .Where(x => x is GraphQLObjectValue)
                .Select(x => x as GraphQLObjectValue),
            GraphQLObjectValue objectValue => [objectValue],
            _ => []
        };
        nodes = (objectValueList ?? [])
            .Select(x => new GraphQlObjectValuePairProvider(x!))
            .ToArray<IFieldNode>();
        return nodes.Length > 0;
    }
}

public record GraphQlObjectValuePairProvider(GraphQLObjectValue ObjectValue) : IFieldNode
{
    public bool TryGetPairs(string fieldName,out StrPair[] pairs)
    {
        pairs = [];
        var field = ObjectValue.Field(fieldName);
        if (field is not null)
        {
            pairs = field.Value.ToPairs();
        }
        return pairs.Length > 0;
    }

    public bool TryGetVal(string fieldName, out string value)
    {
        value = "";
        var field = ObjectValue.Field(fieldName);
        return field is not null && field.Value.ToPrimitiveString(out value);
    }
}