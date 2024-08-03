using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.Qs;

public record Pair
{
   public string Key { get; set; } = "";
   public string[] Values { get; set; } = [];
}

public class QsDict
{
   private readonly Dictionary<string,List<Pair>> _dictionary = new ();

   public Dictionary<string,List<Pair>> Dict => _dictionary;


   public QsDict(Dictionary<string, StringValues> dictionary)
   {
      foreach ( var item in dictionary)
      {
         var parts = item.Key.Split('[');
         if (parts.Length != 2)
         {
            continue;
         }

         var (key, sub) = (parts[0], parts[1]);
         if (sub.Length > 0)
         {
            sub = sub[..^1];
         }

         if (!_dictionary.ContainsKey(key))
         {
            _dictionary[key] = [];
         }

         var pair = new Pair
         {
            Key = sub,
            Values = item.Value.Select(x=>x ??"").ToArray()
         };
         _dictionary[key].Add(pair);
      }
   } 
}