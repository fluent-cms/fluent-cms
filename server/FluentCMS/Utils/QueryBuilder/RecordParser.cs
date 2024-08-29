using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;
using System.Collections.Generic;
using System.Text.Json;

public static class RecordParser 
{
    public static Result<Record> Parse (JsonElement jsonElement, Entity entity, Func<Attribute, string, object> cast)
    {
        Dictionary<string, object> ret = new();
        foreach (var property in jsonElement.EnumerateObject())
        {
            var attribute = entity.Attributes.FindOneAttribute(property.Name);
            if (attribute == null) continue;
            var res = attribute.Type switch
            {
                DisplayType.lookup => SubElement(property.Value, attribute.Lookup!.PrimaryKey).Bind(x=>Convert(x, attribute)),
                _ => Convert(property.Value, attribute)
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            ret[property.Name] = res.Value;
        }
        return ret;
        
        Result<JsonElement?> SubElement(JsonElement element, string key)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out JsonElement subElement))
            {
                return subElement;
            }

            return Result.Ok<JsonElement?>(null!);
        }
        
        Result<object> Convert(JsonElement? element, Attribute attribute)
        {
            if (element is null)
            {
                return Result.Ok<object>(null!);
            }
            return element.Value.ValueKind switch
            {
                JsonValueKind.String => cast(attribute, element.Value.GetString()!),
                JsonValueKind.Number when element.Value.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.Value.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.Value.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.Number => element.Value.GetDecimal(),
                JsonValueKind.True => Result.Ok<object>(true),
                JsonValueKind.False => Result.Ok<object>(false),
                JsonValueKind.Null => Result.Ok<object>(null!),
                JsonValueKind.Undefined => Result.Ok<object>(null!),
                _ => Result.Fail<object>($"value kind {element.Value.ValueKind} is not supported")
            };
        }
    }
}

