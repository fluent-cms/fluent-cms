using FluentResults;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.DictionaryExt;

public static class DictionaryExt
{
    public static bool GetStrings(this StrArgs? args, string key, out string[] strings)
    {
        strings = [];
        if (args is null || !args.TryGetValue(key, out var vals))
        {
            return false;
        }

        strings = vals.Where(s => s is not null).Select(s => s!).ToArray();
        return true;
    }

    public static bool TryGetInt(this StrArgs dictionary, string key, out int intValue)
    {
        intValue = 0;
        if (!dictionary.TryGetValue(key, out var value) || !int.TryParse(value.ToString(), out var result))
            return false;
        intValue = result;
        return true;
    }

    public static Dictionary<Tk, Tv> MergeByOverwriting<Tk, Tv>(this Dictionary<Tk, Tv> a, Dictionary<Tk, Tv> b)
        where Tk : notnull
    {
        var ret = new Dictionary<Tk, Tv>(a);
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
    public static Dictionary<string, StrArgs> GroupByFirstIdentifier(
        this StrArgs dictionary, string startDelimiter = "[", string endDelimiter = "]")
    {
        var result = new Dictionary<string, StrArgs>();
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