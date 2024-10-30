using FluentResults;

namespace FluentCMS.Utils.DictionaryExt;

public static class DictionaryExt
{
    public static Result<object> GetValue(this IDictionary<string, object> dictionary, string key)
    {
        var parts = key.Split('.');
        object current = dictionary;

        // Traverse through the nested dictionaries
        foreach (var part in parts)
        {
            if (current is IDictionary<string, object> dict && dict.TryGetValue(part, out var value))
            {
                current = value;
            }
            else
            {
                return Result.Fail($"can not find {key} in dictionary");
            }
        }
        return current;
    }
   
}