using FluentCMS.Models.Queries;

namespace FluentCMS.Models;

public class Settings
{
    public Entity? Entity { get; set; }
    public Menu[]? Menus { get; set; }
}