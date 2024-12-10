namespace FluentCMS.Utils.QueryBuilder;

//the reason not ues object directly
//1. ensure value can not be one of the primitive type in the construction
//2. after json serialize deserialize, object was changed to JsonElement
public readonly record struct ValidValue(string S = "", int? I = default, long ? L = default, DateTime? D = default)
{
    public object Value => I as object ?? D as object ?? L as object?? S;
}

public static class ValidValueExtensions
{
    public static ValidValue ToValidValue(this object o) => o switch
    {
        string s => new ValidValue(s),
        int i => new ValidValue(I:i),
        long l => new ValidValue(L:l),
        DateTime d => new ValidValue(D:d),
        _ => new ValidValue(o.ToString()??"")
    };  
    
    public static bool IsEmpty(this ValidValue validValue) => validValue.Value is "";
    public static object[] GetValues(this IEnumerable<ValidValue> values) => [..values.Select(v => v.Value)];
}
