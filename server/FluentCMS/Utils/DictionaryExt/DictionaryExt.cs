using FluentResults;
using Microsoft.Extensions.Primitives;

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

    /* 
     * convert 
     * {
     *      name[startsWidth]: a,
     *      name[endsWith]: b,
     * }
     * to
     * {
     *      name : {
     *          startsWidth : a,
     *          endsWith : b
     *      }
     * }
     */
    public static Dictionary<string, Dictionary<string, StringValues>> GroupByFirstIdentifier(
        this Dictionary<string, StringValues> dictionary)
    {
        var result = new Dictionary<string, Dictionary<string, StringValues>>();
        foreach (var (key,value) in dictionary)
        {
            var parts = key.Split('[');
            if (parts.Length != 2)
            {
                continue;
            }

            var (k, subKey)= (parts[0], parts[1]);
            if (!subKey.EndsWith("]"))
            {
                continue;
            }
            subKey = subKey[..^1];
            if (!result.ContainsKey(k))
            {
                result[k] = new();
            }

            result[k][subKey] = value;
        }
        return result;
    }
   
}