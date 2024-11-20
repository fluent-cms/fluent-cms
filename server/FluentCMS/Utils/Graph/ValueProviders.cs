using FluentCMS.Utils.QueryBuilder;
using GraphQL.Execution;
using GraphQLParser.AST;

namespace FluentCMS.Utils.Graph;

public record GraphQlArgumentValueProvider(GraphQLArgument Argument) : IValueProvider,INameProvider, IPairProvider
{
    public string Name()
    {
        return Argument.Name.StringValue;
    }

    public bool Vals(out  string[] array)
    {
        array = Argument.Value switch
        {
            GraphQLListValue listValue => listValue.ToPrimeStrings(),
            _ => Argument.Value.ToPrimitiveString(out var s) ? [s]:[]  
        };
        return array.Length > 0;
    }
    
    public bool Pairs(out  (string,object)[] pairs)
    {
        pairs = Argument.Value switch
        {
            GraphQLObjectValue objectValue => objectValue.ToPairs(),
            GraphQLListValue listValue => listValue.ToPairs(), 
            _ => []
        };
        return pairs.Length > 0;
    }

    public bool Objects(out Record[] objects)
    {
        throw new NotImplementedException();
    }
}

public record ArgumentKeyValueProvider(string Key, ArgumentValue Value) : INameProvider,IValueProvider, IPairProvider, IObjectProvider
{
    public string Name()
    {
        return Key;
    }

    public bool Vals(out string[] array)
    {
        array = Value.Value switch
        {
            object[] vals => [..vals.Select(x => x.ToString()!)],
            _ => []
        };
        return array.Length > 0;
    }

    public bool Pairs(out (string, object)[] pairs)
    {
        pairs = [];
        return Value.Value is not null && DictionaryExt.DictionaryExt.DictObjsToPair(Value.Value, out pairs);
    }

    public bool Objects(out Record[] objects)
    {
        objects = [];
        if (Value.Value is not object[] valuesObjects)
        {
            return false;
        }
        var ret = new List<Record>();
        foreach (var valuesObject in valuesObjects)
        {
            if (valuesObject is not Dictionary<string, object> dictionary) return false;
            ret.Add(dictionary);
        }
        objects = ret.ToArray();
        return objects.Length > 0;
    }
}

public record FilterDictPairProvider(Record record) : IPairProvider,INameProvider
{
    public string Name() => record.TryGetValue(FilterConstants.FieldKey, out var value) ? value.ToString()! : "";

    public bool Pairs(out (string, object)[] pairs)
    {
        pairs = [];
        return record.TryGetValue(FilterConstants.ClauseKey, out var clause) &&
               DictionaryExt.DictionaryExt.DictObjToPair(clause, out pairs);
    }
}