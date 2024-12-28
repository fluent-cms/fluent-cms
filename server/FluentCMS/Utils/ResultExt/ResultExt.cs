using FluentResults;

namespace FluentCMS.Utils.ResultExt;

/// <summary>
/// Represents an exception that is deliberately thrown to notify a client about a specific error.
/// </summary>
public class ResultException(string message) : Exception(message)
{
    public static async Task<T> Try<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch(Exception ex)
        {
            throw new ResultException(ex.Message);  
        }
    }

    public static async Task Try(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            throw new ResultException(ex.Message);
        }
    }
}

public static class ResultExt
{
    public static Result<T> OnFail<T>(this Result<T> result, string message)
    {
        if (result.IsFailed)
        {
            result.Errors.Insert(0, new Error(message));
        }

        return result;
    }

    public static async Task<Result<T>> OnFail<T>(this Task<Result<T>> res, string message)
    {
        var result = await res;
        if (result.IsFailed)
        {
            result = Result.Fail([new Error(message), ..result.Errors]);
        }
        return result;
    }

    public static Result PipeAction<TValue>(this Result<TValue> res, Action<TValue> action)
    {
        return res.Bind(x =>
        {
            action(x);
            return Result.Ok();
        });
    }
    
    public static bool Try<T>(this Result<T> res, out T val, out List<IError>? err)
    {
        (var ok, _, val,  err) = res;
        return ok;
    }

    public static bool Try(this Result res, out List<IError>? err)
    {
        (var ok, _, err) = res;
        return ok;
    }
    
    
    public static Result<TTarget[]> ShortcutMap<TSource, TTarget>(this IEnumerable<TSource> items,Func<TSource,Result<TTarget> > mapper)
    {
        var ret = new List<TTarget>();
        foreach (var s in items)
        {
            var res = mapper(s);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            ret.Add(res.Value);
        }
        return ret.ToArray();
    }
    
    /// <summary>
    /// Throws a <see cref="ResultException"/> if the result indicates failure.
    /// Use this method to terminate the current execution flow and return an error message to the client.
    /// Recommended for use in test projects or outer layers of the application.
    /// </summary>
    public static void Ok(this Result? result)
    {
        if (result is not null && result.IsFailed)
        {
            throw new ResultException($"{string.Join(";",result.Errors.Select(e =>e.Message))}");
        }
    }
    
    public static T Ok<T>(this Result<T> result)
    {
        return result switch
        {
            { IsFailed: true } => throw new ResultException($"{string.Join(";",result.Errors.Select(x=>x.Message))}"),
            _ => result.Value
        };
    }

    public static async Task<T> Ok<T>(this Task<Result<T>> t)
    {
        var result = await t;
        return result switch
        {
            { IsFailed: true } => throw new ResultException($"{string.Join(";",result.Errors.Select(x=>x.Message))}"),
            _ => result.Value
        }; 
    }
}