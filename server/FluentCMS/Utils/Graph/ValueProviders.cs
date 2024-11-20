using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;

namespace FluentCMS.Utils.Graph;

public record GraphQlArgumentValueProvider(GraphQLArgument Argument) : IValueProvider
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public bool Vals(out  ImmutableArray<string> array)
    {
        array = Argument.Value switch
        {
            GraphQLListValue listValue => listValue.ToPrimeStrings(),
            _ => Argument.Value.ToPrimitiveString(out var s) ? [s]:[]  
        };
        return !array.IsEmpty;
    }
    
    public bool Pairs(out  ImmutableArray<(string,object)> pairs)
    {
        pairs = Argument.Value switch
        {
            GraphQLObjectValue objectValue => objectValue.ToPairs(),
            GraphQLListValue listValue => listValue.ToPairs(), 
            _ => ImmutableArray<(string,object)>.Empty
        };
        return !pairs.IsEmpty;
    }
}

public record ArgumentKeyValueValueProvider(string Key, ArgumentValue Value) : IValueProvider
{
    public string Name()
    {
        return Key;
    }

    public bool Vals(out ImmutableArray<string> array)
    {
        array = Value.Value switch
        {
            object[] vals => [..vals.Select(x => x.ToString()!)],
            _ => ImmutableArray<string>.Empty
        };
        return !array.IsEmpty;
    }

    public bool Pairs(out ImmutableArray<(string, object)> pairs)
    {
        pairs = [];
        if (Value.Value is not object[] valuesObjects)
        {
            return false;
        }
        
        var ret = new List<(string, object)>();
        foreach (var valuesObject in valuesObjects)
        {
            if (valuesObject is not Dictionary<string, object> dictionary) return false;
            foreach (var (key, value) in dictionary)
            {
                ret.Add((key,value));
            }
        }
        
        pairs = [..ret];
        return !pairs.IsEmpty;
    }
}