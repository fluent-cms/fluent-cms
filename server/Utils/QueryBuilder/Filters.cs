using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;
using SqlKata;

namespace Utils.QueryBuilder;
using System.Collections.Generic;

public sealed class Filter
{
    private const string Pre = "f[";
    private const string End = "][operator]";
    private const string ConstraintValue = "[constraints][value]";
    private const string ConstraintMathMode = "[constraints][matchMode]";

    private string _fieldName = "";
    public string FieldName
    {
        get => _fieldName;
        set => _fieldName = NameFormatter.LowerNoSpace(value);
    }

    public string Operator { get; set; } = "";

    public List<Constraint> Constraints { get; set; }

    public Filter()
    {
        Constraints = new List<Constraint>();
    }
    public static bool Parse(Dictionary<string, StringValues> qs, string key, StringValues val, out Filter filter)
    {
        filter = new Filter();
        if (!(key.StartsWith(Pre) && key.EndsWith(End)))
        {
            return false;
        }
        
        var op = val.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(op))
        {
            return false;
        }
        
        filter = new Filter
        {
            FieldName = key.Substring(Pre.Length, key.Length - Pre.Length - End.Length),
            Operator = op
        };

        if (!qs.TryGetValue(Pre + filter.FieldName + "]" + ConstraintValue, out var values))
        {
            return false;
        }

        if (!qs.TryGetValue(Pre + filter.FieldName + "]" + ConstraintMathMode, out var mods))
        {
            return false;
        }

        if (values.Count != mods.Count)
        {
            return false;
        }

        for (var i = 0; i < values.Count; i++)
        {
            var v = values[i];
            var m = mods[i];
            if (v is null || m is null)
            {
                return false;
            }
            var cons = new Constraint
            {
                Value = v,
                Match = m,
            };
            filter.Constraints.Add(cons);
        }
        return true;
    }

    public void Apply(Entity entity, Query query)
    {
        var fieldName = entity.Fullname(FieldName);
        switch (Operator)
        {
            case "or":
                query.Or();
                break;
            case "not":
                query.Not();
                break;
        }
        foreach (var constraint in Constraints)
        {
            constraint.Apply(query,fieldName);
        }
    }

}

public class Filters : List<Filter>
{
    private const string QuerystringPrefix = "querystring.";
    private const string TokenPrefix = "token.";
    public Filters(){}
    public Filters(Dictionary<string, StringValues> queryStringDictionary)
    {
        foreach (var pair in queryStringDictionary)
        {
            if (Filter.Parse(queryStringDictionary, pair.Key, pair.Value, out var filter))
            {
                Add(filter);
            }
        }
    }

    public void Resolve(Entity entity, Dictionary<string, StringValues>? querystringDictionary,
        Dictionary<string, object>? tokenDictionary)
    {
        foreach (var filter in this)
        {
            var field = QueryExceptionChecker
                .NotNull(entity.FindOneAttribute(filter.FieldName))
                .ValueOrThrow($"Fail to resolve filter: no field ${filter.FieldName} in ${entity.Name}");
            foreach (var filterConstraint in filter.Constraints)
            {
                filterConstraint.ResolvedValues = QueryExceptionChecker
                        .StrNotEmpty(filterConstraint.Value)
                        .ValueOrThrow($"Fail to resolve Filter, value not set for field{field.Field}") switch
                    {
                        var s when s.StartsWith(QuerystringPrefix) => ResolveQuerystringPrefix(field, s),
                        var s when s.StartsWith(TokenPrefix) => ResolveTokenPrefix(s),
                        var s => [field.CastToDatabaseType(s)]
                    };
            }
        }

        return;
        
        object[] ResolveQuerystringPrefix(Attribute field, string val)
        {
            var key = val[QuerystringPrefix.Length..];
            var querystringDictionaryChecked = QueryExceptionChecker
                .NotNull(querystringDictionary)
                .ValueOrThrow($"Fail to resolve filter: no key {key} in query string");

            return querystringDictionaryChecked[key].Select(x =>
                field.CastToDatabaseType(QueryExceptionChecker.StrNotEmpty(x)
                    .ValueOrThrow($"Fail to resolve filter: {key} not found in attribute"))).ToArray();
        }

        object[] ResolveTokenPrefix(string val)
        {
            // Implement the logic for resolving TokenPrefix here
            throw new NotImplementedException();
        }
    }

    public void Apply(Entity entity, Query? query)
    {
        if (query is null)
        {
            return;
        }

        foreach (var filter in this)
        {
            filter.Apply(entity, query);
        }
    }
    
   
}
