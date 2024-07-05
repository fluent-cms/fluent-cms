using System.Text.Json.Serialization;

namespace Utils.QueryBuilder;

public class View
{
    public string Name { get; set; }
    public string[]? AttributeNames { get; set; } // if not set default to entity list attribute
    public string? EntityName { get; set; }
    public int PageSize { get; set; }

    
    [JsonIgnore]
    public Entity? Entity { get; set; }
    
    public Sorts? Sorts { get; set; }
    public Filters? Filters { get; set; }

    public View()
    {
        Sorts = new Sorts();
        Filters = new Filters();
    }
}