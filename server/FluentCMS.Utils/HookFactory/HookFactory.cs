using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentCMS.Utils.QueryBuilder;
using Microsoft.Extensions.DependencyInjection;
using SqlKata;

namespace FluentCMS.Utils.HookFactory;
public partial class HookFactory
{
    private readonly  List<Hook> _hooks= new ();

    public void AddHook(string entityName, Occasion occasion, Next next, Delegate func)
    {
        _hooks.Add(new Hook
        {
            EntityName = entityName,
            Occasion = occasion,
            Next = next,
            Callback = func
        });
    }
    
    public async Task ExecuteAfterQuery(IServiceProvider provider, Occasion occasion,
        string entityName, ListResult listResult)
    {
        foreach (var hook in GetHooks(entityName, occasion))
        {
            await hook.ModifyListResult(provider, listResult);
            if (hook.Next == Next.Exit)
            {
                break ;
            }
        }
    }
    
    public async Task<(object?, Next)> ExecuteBeforeQuery(IServiceProvider provider, Occasion occasion,
        string entityName, Filters filters, Sorts sorts, Pagination pagination)
    {
        var next = Next.Continue;
        object? ret = null; 
        foreach (var hook in GetHooks(entityName, occasion))
        {
            ret = await hook.ModifyQuery(provider, filters,sorts,pagination);
            next = hook.Next;
            if (hook.Next == Next.Exit)
            {
                break ;
            }
        }

        return (ret,next);
    }

    public async Task<(Record,Next)> ExecuteRecordToRecord(IServiceProvider provider, Occasion occasion, 
        string entityName, Record record)
    {
        var next = Next.Continue;
        foreach (var hook in GetHooks(entityName, occasion))
        {
            record = await hook.RecordToRecord(provider, record);
            next = hook.Next;
            if (hook.Next == Next.Exit)
            {
                break;
            }
        }
        return (record,next);
    }
    
    public async Task<(object, Next)> ExecuteStringToObject(IServiceProvider provider, Occasion occasion, 
        string entityName, string str)
    {
        var next = Next.Continue;
        object ret = str;
        foreach (var hook in GetHooks(entityName, occasion))
        {
            ret  = await hook.ObjectToObject(provider, ret);
            next = hook.Next;
            if (next == Next.Exit)
            {
                break;
            }
        }
        return (ret,next);
    }

    public async Task<(object, Next)> ExecuteRecordToObject(IServiceProvider provider, Occasion occasion,
        string entityName, Record recordData)
    {
        object item = recordData;
        var next = Next.Continue;
        foreach (var hook in GetHooks(entityName, occasion))
        {
            next = hook.Next;
            item = await hook.ObjectToObject(provider, item);
            if (hook.Next == Next.Exit)
            {
                break;
            }
        }

        return (item, next);
    }


    private Hook[] GetHooks(string entityName, Occasion occasion) =>
        _hooks.Where(x => x.Occasion == occasion && x.EntityName == entityName).ToArray();
    

}