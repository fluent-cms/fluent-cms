namespace FormCMS.Utils.EnumExt;

public static class EnumExtensions
{
    public static string ToCamelCase(this Enum enumValue)
    {
        var stringValue = enumValue.ToString();
        if (string.IsNullOrEmpty(stringValue)) return stringValue;
        return char.ToLowerInvariant(stringValue[0]) + stringValue[1..];
    }
}