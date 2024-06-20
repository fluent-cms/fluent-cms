using System.Reflection;
using System.Text.Json;
using LanguageExt;

namespace FluentCMS.Models.Queries;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class Record : Dictionary<string, object>
{
    public Record(JsonElement jsonElement) : base()
    {
        foreach (JsonProperty property in jsonElement.EnumerateObject())
        {
            this[property.Name] = ConvertJsonElement(property.Value);
        }
    }

    private object ConvertJsonElement(JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                return new Record(jsonElement);

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in jsonElement.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return jsonElement.GetString();

            case JsonValueKind.Number:
                if (jsonElement.TryGetInt32(out int intValue))
                    return intValue;
                if (jsonElement.TryGetInt64(out long longValue))
                    return longValue;
                if (jsonElement.TryGetDouble(out double doubleValue))
                    return doubleValue;
                return jsonElement.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return jsonElement.GetRawText();
        }
    }
}

