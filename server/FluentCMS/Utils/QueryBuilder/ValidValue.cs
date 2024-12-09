namespace FluentCMS.Utils.QueryBuilder;

public record struct ValidValue(string S = "", int? I = default, DateTime? D = default)
{
    public object Value => I as object ?? D as object ?? S;
}

public static class ValidValueExtensions
{
    public static bool IsEmpty(this ValidValue validValue) => validValue.Value is "";
    public static object[] GetValues(this IEnumerable<ValidValue> values) => [..values.Select(v => v.Value)];
}
