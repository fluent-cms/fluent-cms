using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentCMS.Core.Descriptors;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions CamelNaming = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    
    public static readonly JsonSerializerOptions IgnoreCase= new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void AddCamelEnumConverter<T>(Microsoft.AspNetCore.Http.Json.JsonOptions options)
        where T: struct, Enum
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase));
    }
}