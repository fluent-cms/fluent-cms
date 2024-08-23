using FluentCMS.Models;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.HookFactory;
public class HookRegistry
{
    private readonly  List<Hook> _hooks= [];

    public void AddHooks(string entityName, Occasion[] occasions, Delegate func)
    {
        foreach (var occasion in occasions)
        {
            _hooks.Add(new Hook
            {
                EntityName = entityName,
                Occasion = occasion,
                Callback = func
            });
        }
    }

    public async Task ModifyListResult(IServiceProvider provider, Occasion occasion,
        RecordMeta meta, ListResult listResult)
    {
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
        {
            var exit = await hook.ModifyListResult(provider,meta, listResult);
            if (exit)
            {
                break ;
            }
        }
    }
    
    public async Task<bool> ModifySchema(IServiceProvider provider, Occasion occasion,
        Schema schema)
    {
        var exit = false;
        foreach (var hook in _hooks.Where(x=>x.Occasion == occasion))
        {
            exit = await hook.ModifySchema(provider, schema);
            if (exit)
            {
                break ;
            }
        }
        return exit;
    }
    public async Task<bool> ModifyQuery(IServiceProvider provider, Occasion occasion,
        RecordMeta meta, Filters filters, Sorts sorts, Pagination pagination)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
        {
            exit = await hook.ModifyQuery(provider,meta, filters,sorts,pagination);
            if (exit)
            {
                break ;
            }
        }

        return exit;
    }
    public async Task<bool> ModifyRelatedRecords(IServiceProvider provider, Occasion occasion, RecordMeta meta, Attribute attribute, Record[] record)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
        {
            exit = await hook.ModifyRelatedRecords(provider,meta, attribute, record);
            if (exit)
            {
                break;
            }
        }

        return exit;
    }

    public async Task<bool> ModifyRecordAndFilter(IServiceProvider provider, Occasion occasion, RecordMeta meta, Record record, Filters filters)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
        {
            exit = await hook.ModifyRecordAndFilter(provider, meta, record,filters);
            if (exit)
            {
                break;
            }
        }

        return exit;
    }

    public async Task<bool> ModifyRecord(IServiceProvider provider, Occasion occasion, RecordMeta meta, Record record)
    {
        var exit = false;
        foreach (var hook in GetHooks(meta.Entity.Name, occasion))
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