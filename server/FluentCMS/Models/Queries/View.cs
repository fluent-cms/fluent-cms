using System.Text.Json.Serialization;

namespace FluentCMS.Models.Queries;

public class View
{
    public string[] AttributeNames { get; set; } // if not set default to entity list attribute
    [JsonIgnore]
    public Attribute[]? Attributes { get; set; }
    public string? EntityName { get; set; }

    
    [JsonIgnore]
    public Entity? Entity { get; set; }
    
    public Sorts? Sorts { get; set; }
    public Filters? Filters { get; set; }

    public View()
    {
        Sorts = new Sorts();
        Filters = new Filters();
    }

    public Attribute[] GetAttributes()
    {
        return Attributes ?? Entity?.GetAttributes(null, Entity.InListOrDetail.InList)??[];
    }
}