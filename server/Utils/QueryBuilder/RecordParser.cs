namespace Utils.QueryBuilder;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class RecordParser 
{
    public static Record Parse (JsonElement jsonElement, Entity entity)
    {
        Dictionary<string, object> ret = new();
        foreach (JsonProperty property in jsonElement.EnumerateObject())
        {
            var field = entity.FindOneAttribute(property.Name);
            if (field != null)
            {
                ret[property.Name] = ConvertJsonElement(property.Value, field);
            }
        }

        return ret;
    }


    private static object ConvertJsonElement(JsonElement element, Attribute attribute)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return attribute.CastToDatabaseType(element.GetString()!);
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

