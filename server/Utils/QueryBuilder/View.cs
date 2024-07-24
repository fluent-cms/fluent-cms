using System.Text.Json.Serialization;

namespace Utils.QueryBuilder;

public sealed class View
{
    public string Name { get; set; } = "";
    public string[]? AttributeNames { get; set; } // if not set default to entity list attribute
    public string EntityName { get; set; } = "";
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
        var entity = QueryExceptionChecker.NotNull(Entity).
            ValueOrThrow($"entity of view {Name} is not initialized");
        return AttributeNames?.Length > 0
            ? entity.LocalAttributes(AttributeNames)
            : entity.LocalAttributes(inListOrDetail);
    }
    
    public Attribute[] GetAttributesByType(DisplayType displayType, InListOrDetail inListOrDetail)
    {
        var entity = QueryExceptionChecker.NotNull(Entity).
            ValueOrThrow($"entity of view {Name} is not initialized");
        
        return AttributeNames?.Length > 0
            ? entity.GetAttributesByType(displayType, AttributeNames)
            : entity.GetAttributesByType(displayType, inListOrDetail);
    }
}