using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.HookFactory;
public class HookRegistry
{
    private readonly  List<Hook> _hooks= new ();

    public void AddHook(string entityName, Occasion occasion,Delegate func)
    {
        _hooks.Add(new Hook
        {
            EntityName = entityName,
            Occasion = occasion,
            Callback = func
        });
    }
    
    public async Task ModifyListResult(IServiceProvider provider, Occasion occasion,
        string entityName, ListResult listResult)
    {
        foreach (var hook in GetHooks(entityName, occasion))
        {
            var exit = await hook.ModifyListResult(provider, listResult);
            if (exit)
            {
                break ;
            }
        }
    }
    
    public async Task<bool> ModifyQuery(IServiceProvider provider, Occasion occasion,
        RecordMeta meta, Filters filters, Sorts sorts, Pagination pagination)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.EntityName, occasion))
        {
            exit = await hook.ModifyQuery(provider, filters,sorts,pagination);
            if (exit)
            {
                break ;
            }
        }

        return exit;
    }

    public async Task<bool> ModifyRecord(IServiceProvider provider, Occasion occasion, RecordMeta meta, Record record)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.EntityName, occasion))
        {
            exit = await hook.ModifyRecord(provider,meta, record);
            if (exit)
            {
                break;
            }
        }

        return exit;
    }
    
    private Hook[] GetHooks(string entityName, Occasion occasion)
    {
        return _hooks.Where(x =>
        {
            var name = x.EntityName;
            if (name.EndsWith("*"))
            {
                name = name.Substring(0, name.Length - 1);
            }
            return x.Occasion == occasion && (name =="" || entityName.StartsWith(name));
        }).ToArray();
    }
}