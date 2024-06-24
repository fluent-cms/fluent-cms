using SqlKata;

namespace FluentCMS.Models.Queries;

public class Constraint
{
    public string Match { get; set; } = "";
    public string Value { get; set; } = "";

    public void Apply(Query query, string field)
    {
        switch (Match)
        {
            case "startsWith":
                query.WhereStarts(field, Value);  
                break;
        }
    }
}