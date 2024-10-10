using System.Text.Json.Serialization;
namespace FluentCMS.Utils.QueryBuilder;

public sealed class Query
{
    public string Name { get; set; } = "";
    public string EntityName { get; set; } = "";
    public int PageSize { get; set; }

    public string SelectionSet { get; set; } = "";
    [JsonIgnore] public Attribute[] Selection { get; set; } = [];
    
    [JsonIgnore]
    public Entity? Entity { get; set; }

    public Sort[] Sorts { get; set; } = [];
    public Filter[] Filters { get; set; } = [];
}