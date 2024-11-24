namespace FluentCMS.Utils.QueryBuilder;

public abstract record ScalarValue;

public record IntValue(int Value) : ScalarValue;
public record StringValue(string Value) : ScalarValue;
public record BooleanValue(bool Value) : ScalarValue;

public record StrPair(string Key, string[] Value);

public interface IFieldNode
{
    bool TryGetVal(string fieldName, out string value);
    bool TryGetPairs(string fieldName, out StrPair[] pairs);
}

public interface IDataProvider
{
    string Name();
    bool TryGetVal(out ScalarValue? value); // idSet: 1
    bool TryGetVals(out string[] values); // idSet:[1]
    bool TryGetPairs(out StrPair[] pairs); //id :{gt: 1}
    bool TryGetNodes (out IFieldNode[] nodes); //expr : {field:'', clauses:[]}
}