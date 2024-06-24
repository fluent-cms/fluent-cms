using Microsoft.Extensions.Primitives;
using SqlKata;

namespace FluentCMS.Models.Queries;
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

    public Filters(){}

    public Filters(Dictionary<string, StringValues> qs)
    {
        foreach (var pair in qs)
        {
            if (Filter.Parse(qs, pair.Key, pair.Value, out var filter))
            {
                Add(filter);
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
