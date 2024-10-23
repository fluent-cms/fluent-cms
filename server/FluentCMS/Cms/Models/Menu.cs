using System.Collections.Immutable;

namespace FluentCMS.Cms.Models;

public sealed record Menu(string Name, ImmutableArray<MenuItem> MenuItems);
public sealed record MenuItem(string Icon, string Label, string Url, bool IsHef = false);