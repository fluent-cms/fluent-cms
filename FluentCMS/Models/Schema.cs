using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FluentCMS.Models;

namespace FluentCMS.Models;

[Table("__schemas")]
public class Schema:ModelBase
{
    [Column(TypeName = "VARCHAR")]
    [StringLength(250)]
    public string Name { get; set; } = "";
    [Column(TypeName = "VARCHAR")]
    [StringLength(250)]
    public string Type { get; set; } = "";
    
    public string Settings { get; set; } = "";
}

public class SchemaDisplayDto : SchemaDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SchemaDisplayDto(){}
    public SchemaDisplayDto(Schema item):base(item)
    {
        Id = item.Id;
        CreatedAt = item.CreatedAt;
        UpdatedAt = item.UpdatedAt;
    }
}

public class SchemaDto
{
    public int ?Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    
    public Settings? Settings { get; set; }
    public SchemaDto(){}

    public SchemaDto(Schema item)
    {
        Name = item.Name;
        Type = item.Type;
        Settings = JsonSerializer.Deserialize<Settings>(item.Settings);
    }
    public Schema ToEntity()
    {
        var item = new Schema();
        Attach(item);
        return item;
    }
    
    public void Attach(Schema item)
    {
        if (Name != null)
        {
            item.Name = Name;
        }
        if (Type != null)
        {
            item.Type = Type;
        }
        if (Settings != null)
        {
            item.Settings = JsonSerializer.Serialize(Settings);
        }
    }
}