using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.HookFactory;

public enum Occasion
{
    BeforeQueryOne,
    AfterQueryOne,
    BeforeQueryMany,
    AfterQueryMany,
    BeforeInsert,
    AfterInsert,
    BeforeUpdate,
    AfterUpdate,
    BeforeDelete,
    AfterDelete
}

public enum Next
{
    Continue,
    Exit
}

public sealed class Hook
{
    public string EntityName { get; init; } = null!;
    public Occasion Occasion { get; init; }
    public Next Next { get; init; }
    public Delegate Callback { get; init; } = null!;

    private string ExceptionPrefix {get => $"Execute Hook Fail [{EntityName} - {Occasion}]: ";}
    internal async Task ModifyListResult(IServiceProvider provider,  ListResult listResult)
    {
        var (method, args,_) = PrepareArgument(provider, targetType =>
        {
            return targetType switch
            {
                _ when targetType == typeof(ListResult) => listResult,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {targetType}")
            };
        });
        await InvokeMethod(method,  args, null);
    }

    internal async Task<object?> ModifyQuery(IServiceProvider provider, Filters filters, Sorts sorts,Pagination pagination)
    {
        var (method, args,_) = PrepareArgument(provider, targetType =>
        {
            return targetType switch
            {
                _ when targetType == typeof(Filters) => filters,
                _ when targetType == typeof(Sorts) => sorts,
                _ when targetType == typeof(Pagination) => pagination,
                _ => throw new HookException($"{ExceptionPrefix}can not resolve type {targetType}")
            };
        });
        return await InvokeMethod(method,  args, null);
    }

    internal async Task<object> ObjectToObject(IServiceProvider provider, object item)
    {
        var (method, args,inputs) = PrepareArgument(provider, targetType =>
        {
            return item switch
            {
                Record objects => targetType == typeof(Record) ? objects : ConvertDictionaryToType(objects, targetType),
                _ => item
            };
        });

        return HookChecker.NotNull(await InvokeMethod(method,  args,inputs.Last()))
            .ValOrThrow($"{ExceptionPrefix} didn't get result from hook, con not proceed");
    }

    internal async Task<Record> RecordToRecord(IServiceProvider provider, Record record)
    {
        var (method, args,inputs) = PrepareArgument(provider, t =>
        {
            return t == typeof(Record) || t== typeof(Dictionary<string,object>) ? record : ConvertDictionaryToType(record, t);
        });

        var result = HookChecker.NotNull(await InvokeMethod(method,  args, inputs.Last()))
            .ValOrThrow($"{ExceptionPrefix} didn't get result from hook, con not proceed");
        return result is Record or Dictionary<string,object>
            ? (Record)result
            : ConvertObjectToDictionary(result);
    }

    private  (MethodInfo, object[], object []) PrepareArgument(IServiceProvider provider, Func<Type, object> getInput)
    {
        var method = HookChecker
            .NotNull(Callback.GetType().GetMethod("Invoke"))
            .ValOrThrow($"{ExceptionPrefix}The hook does not have an Invoke method.");

        var parameters = method.GetParameters();
        List<object> args = [];
        List<object> inputs = [];
        foreach (var parameterInfo in parameters)
        {
            var service = provider.GetService(parameterInfo.ParameterType);
            if (service is null)
            {
                var input = getInput(parameterInfo.ParameterType);
                inputs.Add(input);
                args.Add(input);
            }
            else
            {
                args.Add(service);
            }
        }
        return (method, args.ToArray(), inputs.ToArray());
    }

    private async Task<object?> InvokeMethod(MethodInfo method, object[] args, object? defaultValue)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType == typeof(Task) ||
                      returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
        var result = isAsync ? await InvokeAsyncTask(method, args.ToArray()) : method.Invoke(Callback, args.ToArray());
        if (method.ReturnType == typeof(Task) || method.ReturnType == typeof(void))
        {
            result = defaultValue;
        }
        return result;
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

    private object ConvertDictionaryToType(Record dictionary, Type targetType)
    {
        var instance = HookChecker.NotNull(Activator.CreateInstance(targetType))
            .ValOrThrow($"{ExceptionPrefix}Fail to convert record to {targetType}");
        foreach (var (key, value) in dictionary)
        {
            var property = targetType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance)
                           ?? targetType.GetProperty(ToTitle(key), BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
            }
        }

        return instance;
    }

    private static Record ConvertObjectToDictionary(object? obj)
    {
        Record dictionary = new Dictionary<string, object>();
        foreach (var property in obj?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [])
        {
            if (!property.CanRead) continue;
            var val = property.GetValue(obj);
            if (val is not null)
            {
                dictionary[ToSnakeCase(property.Name)] = val;
            }
        }

        return dictionary;
    }

    //hello_word to HelloWorld
    private static string ToTitle(string s) =>
        string.Join("", s.Split("_").Select(p => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p)).ToArray());

    //HelloWorld to hello_word
    private static string ToSnakeCase(string s) => Regex.Replace(s, "([a-z])([A-Z])", "$1_$2").ToLower();
}