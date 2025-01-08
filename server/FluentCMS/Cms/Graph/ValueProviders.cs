using FluentCMS.Core.Descriptors;
using FluentCMS.Utils.GraphTypeConverter;
using GraphQLParser.AST;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Cms.Graph;

public record GraphArgument(GraphQLArgument Argument) : IArgument
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public bool TryGetStringArray(out string?[] array)
    {
        array = Argument.Value switch
        {
            GraphQLListValue listValue => Converter.ToPrimitiveStrings(listValue,QueryConstants.VariablePrefix),
            _ => Converter.ToPrimitiveString(Argument.Value,QueryConstants.VariablePrefix,out var s) ? [s] : []
        };
        return array.Length > 0;
    }

    public bool TryGetString(out string? value)
    {
        return Converter.ToPrimitiveString(Argument.Value,QueryConstants.VariablePrefix,out value);
    }

    public bool TryGetDict(out StrArgs dict)
    {
        dict = Argument.Value is GraphQLNullValue
            ? new StrArgs{{"",new StringValues([null])}}
            : Converter.ToDict(Argument.Value,QueryConstants.VariablePrefix);
        
        return dict.Count > 0;
    }

    public bool TryGetObjects(out IObject[] nodes)
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
            .Select(x => new GraphObject(x!))
            .ToArray<IObject>();
        return nodes.Length > 0;
    }
}

public record GraphObject(GraphQLObjectValue ObjectValue) : IObject
{
    public bool TryGetString(string fieldName, out string value)
    {
        value = "";
        var field = ObjectValue.Field(fieldName);
        return field is not null && Converter.ToPrimitiveString(field.Value,QueryConstants.VariablePrefix ,out value);
    }

    public bool TryGetDict(string fieldName, out StrArgs dict)
    {
        dict = [];
        var field = ObjectValue.Field(fieldName);
        if (field is not null)
        {
            dict = Converter.ToDict(field.Value, QueryConstants.VariablePrefix);
        }

        return dict.Count > 0;
    }
}