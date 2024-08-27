using System.Reflection;
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
    
    BeforeQueryView,
    BeforeQueryOneView,
    BeforeQueryManyView,
}

public sealed class Hook
{
    public string SchemaName { get; init; } = null!;
    public Occasion Occasion { get; init; }
    public Delegate Callback { get; init; } = null!;

    private string ExceptionPrefix => $"Execute Hook Fail [{SchemaName} - {Occasion}]: ";

    internal async Task<bool> Trigger(IServiceProvider provider,
        EntityMeta? meta, ViewMeta? viewMeta, SchemaMeta? schemaMeta,
        HookParameter parameter, HookReturn? hookReturn)
    {
        var (method, args) = PrepareArgument(provider, t =>
        {
            return t switch
            {
                _ when t == typeof(EntityMeta) && meta is not null=> meta,
                _ when t == typeof(ViewMeta) && viewMeta is not null=> viewMeta,
                _ when t == typeof(SchemaMeta) && schemaMeta is not null=> schemaMeta,
                
                //list or view
                _ when t == typeof(Filters) && parameter.Filters is not null=> parameter.Filters,
                _ when t == typeof(Sorts) && parameter.Sorts is not null=> parameter.Sorts,
                _ when t == typeof(Pagination) && parameter.Pagination is not null=> parameter.Pagination,
                _ when t == typeof(Cursor) && parameter.Cursor is not null=> parameter.Cursor,
                
                //cross table
                _ when t == typeof(Attribute)  && parameter.Attribute is not null=> parameter.Attribute,
                _ when t == typeof(IList<Record>)  && parameter.Records is not null=> parameter.Records,
                
                //crud query One
                _ when t == typeof(Record) && parameter.Record is not null  => parameter.Record,
                
                //list
                _ when t == typeof(ListResult) && parameter.ListResult is not null  => parameter.ListResult,
                
                //view or before queryOne
                _ when t == typeof(HookReturn) && hookReturn is not null => hookReturn,
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