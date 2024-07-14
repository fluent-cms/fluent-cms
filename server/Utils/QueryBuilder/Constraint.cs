using System.Text.Json.Serialization;
using SqlKata;

namespace Utils.QueryBuilder;

public sealed class Constraint
{
    public string Match { get; set; } = "";
    public string Value { get; set; } = "";

    public object[]? ResolvedValues { get; set; }

    public object GetValue()
    {
        return ResolvedValues?.FirstOrDefault() ?? Value;
    }
    public void Apply(Query query, string field)
    {
        switch (Match)
        {
            case "startsWith":
                query.WhereStarts(field, GetValue());  
                break;
            case "in":
                query.WhereIn(field, ResolvedValues);
                break;
        }
    }
}