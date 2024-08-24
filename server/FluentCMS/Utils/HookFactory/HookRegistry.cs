using FluentCMS.Models;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.HookFactory;
public sealed class HookParameter
{
    public Filters? Filters { get; init; }
    public Sorts? Sorts { get; init; }
    public Pagination? Pagination { get; init; }
    public IList<Record>? Records { get; init; } 
    public Record? Record { get; init; }
    public Attribute? Attribute { get; init; }
    public Schema? Schema { get; init; }
    public ListResult? ListResult { get; init; }
}

public class HookRegistry
{
    private readonly  List<Hook> _hooks= [];

    public void AddHooks(string schemaName, Occasion[] occasions, Delegate func)
    {
        foreach (var occasion in occasions)
        {
            _hooks.Add(new Hook
            {
                SchemaName = schemaName,
                Occasion = occasion,
                Callback = func
            });
        }
    }

    public async Task<bool> Trigger(IServiceProvider provider, Occasion occasion, SchemaMeta schema)
    {
        var exit = false;
        foreach (var hook in _hooks.Where(x=>x.Occasion == occasion))
        {
            exit = await hook.Trigger(provider,null, null,schema, new HookParameter(),null);
            if (exit)
            {
                break ;
            }
        }
        return exit;
    }
    
    public async Task<bool> Trigger(IServiceProvider provider, Occasion occasion, ViewMeta meta, HookParameter hookParameter, HookReturn? hookReturn = null)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.View.Name,occasion))
        {
            exit = await hook.Trigger(provider,null,meta, null,hookParameter, hookReturn);
            if (exit)
            {
                break ;
            }
        }
        return exit;
    }
    
    public async Task<bool> Trigger(IServiceProvider provider, Occasion occasion, EntityMeta meta, HookParameter hookParameter, HookReturn? hookReturn = null)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
        {
            exit = await hook.Trigger(provider,meta,null,null, hookParameter,hookReturn);
            if (exit)
            {
                break ;
            }
        }
        return exit;
    }
    
    private Hook[] GetHooks(string schemaName, Occasion occasion)
    {
        return _hooks.Where(x =>
        {
            var name = x.SchemaName;
            if (name.EndsWith("*"))
            {
                name = name.Substring(0, name.Length - 1);
            }
            return x.Occasion == occasion && (name =="" || schemaName.StartsWith(name));
        }).ToArray();
    }
}