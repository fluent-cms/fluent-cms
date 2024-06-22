namespace FluentCMS.Models.Queries;
using System;
using System.Collections.Generic;
using System.Text.Json;

using Record = IDictionary<string,object>;

public class RecordParser 
{
    public static Record Parse (JsonElement jsonElement, Func<string,Func<string,object>?>getStringCaster)
    {
        Dictionary<string, object> ret = new();
        foreach (JsonProperty property in jsonElement.EnumerateObject())
        {
            var caster = getStringCaster(property.Name);
            if (caster != null)
            {
                ret[property.Name] = ConvertJsonElement(property.Value, caster);
            }
        }

        return ret;
    }


    private static object ConvertJsonElement(JsonElement element, Func<string,object> stringCastFunc)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return stringCastFunc(element.GetString()!);
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue;
                if (element.TryGetInt64(out long longValue))
                    return longValue;
                if (element.TryGetDouble(out double doubleValue))
                    return doubleValue;
                return element.GetDecimal(); 
        }
        return null!;
    }
}

