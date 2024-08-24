using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.HookFactory;

public class EntityMeta
{
    public Entity Entity { get; init; } = null!;
    public string Id { get; set; } = "";
}