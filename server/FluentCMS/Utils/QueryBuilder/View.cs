using System.Text.Json.Serialization;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

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

    public Result<Attribute[]> LocalAttributes(InListOrDetail inListOrDetail)
    {
        if (Entity is null)
        {
            return Result.Fail($"entity of view {Name} is not initialized");
        }

        return AttributeNames?.Length > 0
            ? Entity.LocalAttributes(AttributeNames)
            : Entity.LocalAttributes(inListOrDetail);
    }

    public Result<Attribute[]> GetAttributesByType(DisplayType displayType, InListOrDetail inListOrDetail)
    {
        if (Entity is null)
        {
            return Result.Fail($"entity of view {Name} is not initialized");
        }
        return AttributeNames?.Length > 0
            ? Entity.GetAttributesByType(displayType, AttributeNames)
            : Entity.GetAttributesByType(displayType, inListOrDetail);
    }
}