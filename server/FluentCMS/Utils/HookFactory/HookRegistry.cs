using FluentCMS.Models;

namespace FluentCMS.Utils.HookFactory;
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

    public async Task<bool> Trigger(IServiceProvider provider, Occasion occasion, SchemaMeta meta, Schema? schema)
    {
        var exit = false;
        foreach (var hook in _hooks.Where(x=>x.Occasion == occasion))
        {
            exit = await hook.Trigger(provider,null, null,meta, new HookParameter{Schema = schema},null);
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
        foreach (var hook in GetHooks(meta.ViewName,occasion))
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
        foreach (var hook in GetHooks(meta.EntityName, occasion))
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