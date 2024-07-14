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

    public Attribute[] LocalAttributes(InListOrDetail inListOrDetail)
    {
        if (Entity is null)
        {
            throw new Exception($"entity of view {Name} is not initialized");
        }
        if (AttributeNames?.Length > 0)
        {
            return Entity.LocalAttributes(AttributeNames);
        }
        return Entity.LocalAttributes(inListOrDetail);
    }
    
    public Attribute[] GetAttributesByType(DisplayType displayType, InListOrDetail inListOrDetail)
    {
        if (Entity is null)
        {
            throw new Exception($"entity of view {Name} is not initialized");
        }
        if (AttributeNames?.Length > 0)
        {
            return Entity.GetAttributesByType(displayType,AttributeNames);
        }
        return Entity.GetAttributesByType(displayType,inListOrDetail);
    }
}