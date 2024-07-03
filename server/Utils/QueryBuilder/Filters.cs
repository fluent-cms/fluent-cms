using Microsoft.Extensions.Primitives;
using SqlKata;

namespace Utils.QueryBuilder;
using System.Collections.Generic;

public class Filter
{
    const string Pre = "f[";
    const string End = "][operator]";
    const string ConstraintValue = "[constraints][value]";
    const string ConstraintMathMode = "[constraints][matchMode]";
    
    public string FieldName { get; set; }

    public string Operator { get; set; }

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

        filter = new Filter
        {
            FieldName = key.Substring(Pre.Length, key.Length - Pre.Length - End.Length),
            Operator = val.First(),
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
            var cons = new Constraint
            {
                Value = values[i],
                Match = mods[i],
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
    
    public void Resolve(Entity entity,Dictionary<string,StringValues>? querystringDictionary, Dictionary<string, object>? tokenDictionary)
    {
        foreach (var filter in this)
        {
            var field = entity.FindOneAttribute(filter.FieldName);
            if (field is null)
            {
                throw new Exception($"Fail to resolve filter: no field ${filter.FieldName} in ${entity.EntityName}");

            }
            foreach (var filterConstraint in filter.Constraints)
            {
                var val = filterConstraint.Value;
                if (val.StartsWith(QuerystringPrefix))
                {
                    var key = val.Substring(QuerystringPrefix.Length);
                    if (querystringDictionary is null)
                    {
                        throw new Exception($"Fail to resolve filter: no key ${key} in querystring");
                    }
                    filterConstraint.ResolvedValues =
                        querystringDictionary[key].Select(x => field.CastToDatabaseType(x)).ToArray();
                    
                }
                else if (val.StartsWith(TokenPrefix))
                {
                    //todo
                }
                else
                {
                    filterConstraint.ResolvedValues = [field.CastToDatabaseType(filterConstraint.Value)];
                }
            }
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
