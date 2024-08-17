using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using FluentCMS.Models;
using FluentCMS.Services;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.HookFactory;

public enum Occasion
{
    BeforeDeleteSchema,
    BeforeSaveSchema,
    
    BeforeQueryOne,
    AfterQueryOne,
    
    BeforeQueryMany,
    AfterQueryMany,
    
    BeforeInsert,
    AfterInsert,
    
    BeforeUpdate,
    AfterUpdate,
    
    BeforeDelete,
    AfterDelete,
    
    BeforeAddRelated,
    AfterAddRelated,
    
    BeforeDeleteRelated,
    AfterDeleteRelated,
}

public sealed class Hook
{
    public string EntityName { get; init; } = null!;
    public Occasion Occasion { get; init; }
    public Delegate Callback { get; init; } = null!;

    private string ExceptionPrefix {get => $"Execute Hook Fail [{EntityName} - {Occasion}]: ";}
    internal async Task<bool> ModifyListResult(IServiceProvider provider,  ListResult listResult)
    {
        var (method, args) = PrepareArgument(provider, targetType =>
        {
            return targetType switch
            {
                _ when targetType == typeof(ListResult) => listResult,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {targetType}")
            };
        });
        return await InvokeMethod(method,  args);
    }

    internal async Task<bool> ModifySchema(IServiceProvider provider, Schema schema)
    {
        var (method, args) = PrepareArgument(provider, targetType =>
        {
            return targetType switch
            {
                _ when targetType == typeof(Schema) => schema,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {targetType}")
            };
        });
        return await InvokeMethod(method,  args);
    }
    
    internal async Task<bool> ModifyQuery(IServiceProvider provider, Filters filters, Sorts sorts,Pagination pagination)
    {
        var (method, args) = PrepareArgument(provider, targetType =>
        {
            return targetType switch
            {
                _ when targetType == typeof(Filters) => filters,
                _ when targetType == typeof(Sorts) => sorts,
                _ when targetType == typeof(Pagination) => pagination,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {targetType}")
            };
        });
        return await InvokeMethod(method,  args);
    }
    
    internal async Task<bool> ModifyRelatedRecords(IServiceProvider provider,RecordMeta meta, Attribute attribute, Record[] records)
    {
        var (method, args) = PrepareArgument(provider, t =>
        {
            return t switch
            {
                _ when t == typeof(RecordMeta) => meta,
                _ when t == typeof(Record[])  => records,
                _ when t == typeof(Attribute)  => attribute,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {t}")
            };
        });

        return await InvokeMethod(method, args);
    }

    internal async Task<bool> ModifyRecord(IServiceProvider provider,RecordMeta meta, Record record)
    {
        var (method, args) = PrepareArgument(provider, t =>
        {
            return t switch
            {
                _ when t == typeof(RecordMeta) => meta,
                _ when t == typeof(Record)  => record,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {t}")
            };
        });

        return await InvokeMethod(method, args);
    }

    private  (MethodInfo, object[]) PrepareArgument(IServiceProvider provider, Func<Type, object> getInput)
    {
        var method = HookChecker
            .NotNull(Callback.GetType().GetMethod("Invoke"))
            .ValOrThrow($"{ExceptionPrefix}The hook does not have an Invoke method.");

        var parameters = method.GetParameters();
        List<object> args = [];
        foreach (var parameterInfo in parameters)
        {
            var service = provider.GetService(parameterInfo.ParameterType);
            if (service is null)
            {
                var input = getInput(parameterInfo.ParameterType);
                args.Add(input);
            }
            else
            {
                args.Add(service);
            }
        }
        return (method, args.ToArray());
    }

    private async Task<bool> InvokeMethod(MethodInfo method, object[] args)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType == typeof(Task) ||
                      returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
        try
        {
            var result = isAsync
                ? await InvokeAsyncTask(method, args.ToArray())
                : method.Invoke(Callback, args.ToArray());

            if (method.ReturnType == typeof(Task) || method.ReturnType == typeof(void))
            {
                result = false;
            }

            return result is bool ? (bool)result : false;
        }
        catch(Exception ex)
        {
            throw ex.InnerException??ex;
        }
    }

    private async Task<object?> InvokeAsyncTask(MethodInfo method, object[] args)
    {
        var task = (Task)method.Invoke(Callback, args)!;
        await task.ConfigureAwait(false);
        if (method.ReturnType == typeof(Task))
        {
            return null;
        }
        var resultProperty = HookChecker.NotNull(task.GetType().GetProperty("Result"))
            .ValOrThrow($"{ExceptionPrefix}Cannot get result property of [{method}]");
        return HookChecker.NotNull(resultProperty.GetValue(task))
            .ValOrThrow($"{ExceptionPrefix}Cannot get result from async hook method[{method}]");
    }
}