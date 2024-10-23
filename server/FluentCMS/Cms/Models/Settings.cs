
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Models;

public sealed record Settings(Entity? Entity = default, Query? Query =default, Menu? Menu =default, Page? Page = default);