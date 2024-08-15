using System.Text.Json;

namespace FluentCMS.Utils.JsonElementExt;

public static class JsonElementExt
{
   public static Dictionary<string, object> ToDictionary(this JsonElement element)
   {
      var dict = new Dictionary<string, object>();

      foreach (var prop in element.EnumerateObject())
      {
         dict[prop.Name] = ConvertJsonElement(prop.Value);
      }

      return dict; 
   }
   
   public static List<object> ToArray(this JsonElement jsonElement)
   {
      var list = new List<object>();

      foreach (var item in jsonElement.EnumerateArray())
      {
         list.Add(ConvertJsonElement(item));
      }

      return list;
   }
   private static object ConvertJsonElement(JsonElement element)
   {
         return (element.ValueKind switch
         {
            JsonValueKind.Object => element.ToDictionary(),
            JsonValueKind.Array => element.ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? (object)i : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => "",
         })!;
   }
}