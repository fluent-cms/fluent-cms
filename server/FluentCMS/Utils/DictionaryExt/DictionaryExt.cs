using System.Text;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.DictionaryExt;

public static class DictionaryExt
{
    public static Record[] ToTree(this Record[] records,string idField, string parentField)
    {
        var parentIdField = parentField;
        var lookup = records.ToDictionary(r => r[idField]);
        var roots = new List<IDictionary<string, object>>();

        foreach (var record in records)
        {
            if (record.TryGetValue(parentIdField, out var parentId) && parentId != null && lookup.TryGetValue(parentId, out var parentRecord))
            {
                if (!parentRecord.ContainsKey("children"))
                {
                    parentRecord["children"] = new List<IDictionary<string, object>>();
                }

                ((List<Record>)parentRecord["children"]).Add(record);
            }
            else
            {
                roots.Add(record);
            }
        }
        
        return roots.ToArray();
    }
    
    public static string ToQueryString(this StrArgs? args)
    {
        if (args == null || args.Count == 0)
            return string.Empty;

        var queryString = new StringBuilder();

        foreach (var kvp in args)
        {
            if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value)) continue;
            if (queryString.Length > 0)
            {
                queryString.Append('&');
            }

            queryString.Append(Uri.EscapeDataString(kvp.Key))
                .Append('=')
                .Append(Uri.EscapeDataString(kvp.Value!));
        }

        return queryString.ToString();
        
    }

    public static StringValues ResolveVariable(this StrArgs dictionary, string? key, string variablePrefix)
    {
        if (key is null || !key.StartsWith(variablePrefix))
            return key ?? StringValues.Empty;

        key = key[variablePrefix.Length..];
        return dictionary.TryGetValue(key, out var val)
            ? val
            : StringValues.Empty;
    }

    public static Dictionary<Tk, Tv> OverwrittenBy<Tk, Tv>(this Dictionary<Tk, Tv> baseDict, Dictionary<Tk, Tv> overWriteDict)
        where Tk : notnull
    {
        var ret = new Dictionary<Tk, Tv>(baseDict);
        foreach (var (k, v) in overWriteDict)
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
        this StrArgs strArgs, string startDelimiter = "[", string endDelimiter = "]")
    {
        var result = new Dictionary<string, StrArgs>();
        foreach (var (key,value) in strArgs)
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