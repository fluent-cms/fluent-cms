namespace FluentCMS.Core.Descriptors;

public interface IObject
{
    //e.g. get field from {field:'', clauses:[{},{}]}
    bool TryGetString(string fieldName, out string value); 
    
    //e.g. get clauses from {field:'', clauses:[{},{}]}
    bool TryGetDict(string fieldName, out StrArgs args);
}

public interface IArgument
{
    string Name();
    bool TryGetString(out string? value); // {idSet: 1}
    bool TryGetStringArray(out string?[] values); // {idSet:[1]}
    bool TryGetDict(out StrArgs args); // {id :{gt: 1}}
    bool TryGetObjects (out IObject[] nodes); // {expr : {field:'', clauses:[]}}
}