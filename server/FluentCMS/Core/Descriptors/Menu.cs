namespace FluentCMS.Core.Descriptors;

public sealed record Menu(string Name, MenuItem[] MenuItems);
public sealed record MenuItem(string Icon, string Label, string Url, bool IsHref = false);