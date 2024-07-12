namespace FluentCMS.Services;

public class InvalidParamException(string message) : Exception(message);

public static class Val
{
    public  static string NotEmpty(string variableName,string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new InvalidParamException($"[{variableName}] should not be empty string");
        }

        return str;
    }

    public static T[] NotEmpty<T>(string variableName, T[]? arr)
    {
        if (arr is null || arr.Length == 0)
        {
            throw new InvalidParamException($"[{variableName}] should not be null or empty");
        }

        return arr;
    }
    public  static T NotNull<T>(string variableName,T? obj) 
    {
        if (obj is null)
        {
            throw new InvalidParamException($"[{variableName}] should not be null");
        }
        return obj;
    }
}