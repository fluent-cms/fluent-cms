using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.DictionaryExt;

public static class DictionaryExt
{
    public static Dictionary<TK, TV> MergeByOverwriting<TK, TV>(this Dictionary<TK, TV> a, Dictionary<TK, TV> b)
        where TK : notnull
    {
        var ret = new Dictionary<TK, TV>(a);
        foreach (var (k, v) in b)
        {
            ret[k] = v;
        }

        return ret;
    }
    public static bool GetValueByPath<T>(this IDictionary<string, object> dictionary, string key, out T? val)
    {
        val = default;
        var parts = key.Split('.');
        object current = dictionary;
        
        foreach (var part in parts)
        {
            if (current is IDictionary<string, object> dict && dict.TryGetValue(part, out var tmp))
            {
                current = tmp;
            }
            else
            {
                return false;
            }
        }

        if (current is T t)
        {
            val = t;
        }
        return true;
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
        this Dictionary<string, StringValues> dictionary, string startDelimiter = "[", string endDelimiter = "]")
    {
        var result = new Dictionary<string, Dictionary<string, StringValues>>();
        foreach (var (key,value) in dictionary)
        {
            var parts = key.Split(startDelimiter);
            if (parts.Length != 2)
            {
                continue;
            }

            var (k, subKey)= (parts[0], parts[1]);
            if (!subKey.EndsWith(endDelimiter))
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