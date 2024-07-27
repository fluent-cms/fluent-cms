namespace Utils.QueryBuilder;
using System.Collections.Generic;
using System.Text.Json;

public static class RecordParser 
{
    public static Record Parse (JsonElement jsonElement, Entity entity)
    {
        Dictionary<string, object> ret = new();
        foreach (var property in jsonElement.EnumerateObject())
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
        return element.ValueKind switch
        {
            JsonValueKind.String => attribute.CastToDatabaseType(element.GetString()!),
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.Number => element.GetDecimal(),
            _ => null!
        };
    }
}

