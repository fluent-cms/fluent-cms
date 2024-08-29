namespace FluentCMS.Cms.Models;
public class Schema
{
    public int Id { get; set; } 
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public Settings Settings { get; set; } = null!;
    public string CreatedBy { get; set; } = "";
}