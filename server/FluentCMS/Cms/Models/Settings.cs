
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Models;

public class Settings
{
    public Entity? Entity { get; set; }
    public Query? Query { get; set; }
    public Menu? Menu { get; set; }
}