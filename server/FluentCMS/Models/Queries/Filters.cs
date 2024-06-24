using Microsoft.Extensions.Primitives;

namespace FluentCMS.Models.Queries;
using System.Collections.Generic;

public class Constraint
{
    public string Match { get; set; } = "";
    public string Value { get; set; } = "";
}

public class Filter
{
    public string FieldName { get; set; }

    public string Operator { get; set; }

    public List<Constraint> Constraints { get; set; }

    public Filter()
    {
        Constraints = new List<Constraint>();
    }
}

public class Filters : List<Filter>
{
    const string Pre = "f[";
    const string End = "][operator]";
    const string ConstraintValue = "[constraints][value]";
    const string ConstraintMathMode = "[constraints][matchMode]";
    public Filters(){}

    public Filters(Dictionary<string, StringValues> qs)
    {
        foreach (var pair in qs)
        {
            var (key, val) = (pair.Key, pair.Value);
            if (key.StartsWith(Pre) && key.EndsWith(End))
            {
                var filter = new Filter
                {
                    FieldName = key.Substring(Pre.Length, pair.Key.Length - Pre.Length - End.Length),
                    Operator = val.First(),
                };

                if (!qs.TryGetValue(Pre + filter.FieldName + "]" + ConstraintValue, out var values))
                {
                    continue;
                }

                if (!qs.TryGetValue(Pre + filter.FieldName + "]" + ConstraintMathMode, out var mods))
                {
                    continue;
                }

                if (values.Count != mods.Count)
                {
                    continue;
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
                Add(filter);
            }
        }
    }
}
