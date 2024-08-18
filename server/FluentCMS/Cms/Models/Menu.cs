namespace FluentCMS.Models;

public sealed class Menu
{
    public string Name { get; set; } = "";
    public MenuItem[] MenuItems { get; set; } = [];
}

public sealed class MenuItem
{
    public string Icon { get; set; } = "";
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsHref { get; set; } = false;
}